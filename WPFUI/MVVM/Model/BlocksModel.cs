using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFUI.Core;

namespace WPFUI.MVVM.Model;

public class BlocksModel : ObservableObject
{
    // Get the function and dll needed for a window to be minimised
    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
    // Set the signal sent to WinAPI that will let the program be minimised on call
    private const int SW_MINIMIZE = 6;
    
    public BlocksModel()
    {
        
    }
    
    // Function that deserialises the blocks json file and returns a dictionary of the blocks
    public static Dictionary<string,Block> GetAllBlocks()
    {
        var blockList = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        return (blockList);
    }
    
    // Gets all blocks that are currently active
    public static List<Block> GetAllActiveBlocks()
    {
        // Get list of all blocks
        var fullBlockList = GetAllBlocks();
        List<Block> activeBlocks = new List<Block>();
        // Loop through each block
        foreach (Block block in fullBlockList.Values)
        {
            // Check if block is active
            if (block.Status)
            {
                if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)"))
                {
                    // Check if the block is active on the current day
                    if (block.Conditions[1].TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                    {
                        activeBlocks.Add(block);
                    }
                }
            }
        }
        return activeBlocks;
    }

    // Function that will return a new block based on input parameters
    public static Block CreateNewBlock(string name, string type, List<string> programs)
    {
        return new Block { Name = name, Status = true, Type = type, Programs = programs, Conditions = new Dictionary<int, BlockCondition>()};
    }
    
    // Function that will add a block to the block json file
    public static void AppendBlockToDatabase(Block block)
    {
        // Get existing blocks
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        // Add the new block to the database
        existingBlocks[block.Name] = block;
        // Write the updated dictionary of blocks to the database
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    // Generates a new block condition that will be put into Block.Conditions
    public static Dictionary<int, BlockCondition> CreateNewBlockCondition(string blocktype, Dictionary<int, BlockCondition> blockConditions, List<string> criteria, List<string> timecriteria)
    {
        // Gets all the IDs of the blocks in the dictionary
        List<int> blockConditionKeys = blockConditions.Keys.ToList();
        // Gets the highest ID and adds 1 to it to get the ID of the new block condition
        int newBlockConditionID;
        if (blockConditionKeys.Count > 0)
        {
            newBlockConditionID = blockConditionKeys.Max() + 1;
        }
        else
        {
            newBlockConditionID = 1;
        }
        // Create the new BlockCondition object
        BlockCondition newBlockCondition = new BlockCondition{ Criteria = criteria, TimeCriteria = timecriteria };
        blockConditions[newBlockConditionID] = newBlockCondition;
        // Return the new dictionary of block conditions
        return blockConditions;
    }

    // Function that will update an existing block in the block json file
    public static void UpdateBlock(string blockName, Block block)
    {
        // Get existing blocks
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        // Overwrite the old version of the block with the new version
        existingBlocks[blockName] = block;
        // Write the updated dictionary of blocks to the database
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    // Function that will delete a block
    public static void DeleteBlock(string name){
        // Get existing blocks and remove the block with the given key
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks.Remove(name);
        // Write the updated dictionary of blocks to the database
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    // Function that runs each second with the current program to check if it needs to be blocked
    public static void CheckAllBlocks(string program, int pid)
    {
        // Go through each block and update the usage in blockstatus.json if the current program is in the block
        var blockList = GetAllBlocks();
        foreach (Block block in blockList.Values)
        {
            if (block.Programs.Contains(program))
            {
                // Updates the database and adds 1 second of usage needs to be added as specified in the parameters
                UpdateBlockStatus(block, program, 1);
            }
        }

        // Go through each block again and check if the current program needs to be blocked
        foreach (Block block in blockList.Values)
        {
            // Makes sure the block is currently active
            if (block.Status)
            {
                // Checks the current program is in the block
                if (block.Programs.Contains(program))
                {
                    // Gets the status of the program in the block (i.e. how much time has been used)
                    Dictionary<string, object> blockStatus = GetBlockStatus();
                    if (block.Type == "Usage Limit (Combined)")
                    {
                        // Calculates how much time has been used by all the programs in the block
                        Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                        int totalUsage = 0;
                        foreach (int value in blockDict.Values)
                        {
                            totalUsage += value;
                        }
                        foreach (BlockCondition blockCondition in block.Conditions.Values)
                        {
                            // Checks if the block is active on the current day by checking if the day is in the time criteria
                            if (blockCondition.TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                            {
                                // Calculates the seconds of the limit by multiplying the hours by 60*60 and the minutes by 60
                                int usageLimit = ((Convert.ToInt32(blockCondition.Criteria[0])*60*60)+(Convert.ToInt32(blockCondition.Criteria[1])*60));
                                // Checks if the block has 10, 30, 60, 300, or 600 seconds left of usage and shows a warning notification
                                switch (usageLimit - totalUsage)
                                {
                                    case 5:
                                    case 10:
                                    case 30:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have " + (usageLimit - totalUsage) + " seconds left of usage for " + program + " and other applications in the " + block.Name + " block."
                                        );
                                        break;
                                    case 60:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 1 minute left of usage for " + program + " and other applications in the " + block.Name + " block."
                                        );
                                        break;
                                    case 300:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 5 minutes left of usage for " + program + " and other applications in the " + block.Name + " block."
                                        );
                                        break;
                                    case 600:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 10 minutes left of usage for " + program + " and other applications in the " + block.Name + " block."
                                        );
                                        break;
                                    default:
                                        break;
                                }
                                // Checks if the usage is greater than the limit
                                if (totalUsage > usageLimit)
                                {
                                    // Terminates the program if the usage is greater than the limit
                                    TerminateProgramByPID(pid);
                                    // Shows a warning notification
                                    ShowWarningNotification(
                                        "TimeTrack Usage Warning",
                                        "You have reached the usage limit for the " + block.Name + " block."
                                    );
                                }
                            }
                        }
                    }
                    else if (block.Type == "Usage Limit (Per App)")
                    {
                        // Gets the usage of each program that is in the block
                        Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                        foreach (BlockCondition blockCondition in block.Conditions.Values)
                        {
                            /// Checks if the block is active on the current day by checking if the day is in the time criteria
                            if (blockCondition.TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                            {
                                int usageLimit = ((Convert.ToInt32(blockCondition.Criteria[0])*60*60)+(Convert.ToInt32(blockCondition.Criteria[1])*60));
                                // Checks if the program has 10, 30, 60, 300, or 600 seconds left of usage and shows a warning notification
                                switch (usageLimit - Convert.ToInt32(blockDict[program]))
                                {
                                    case 5:
                                    case 10:
                                    case 30:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have " + (usageLimit - Convert.ToInt32(blockDict[program])) + " seconds left of usage for " + program + " in the " + block.Name + " block."
                                        );
                                        break;
                                    case 60:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 1 minute left of usage for " + program + " in the " + block.Name + " block."
                                        );
                                        break;
                                    case 300:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 5 minutes left of usage for " + program + " in the " + block.Name + " block."
                                        );
                                        break;
                                    case 600:
                                        ShowWarningNotification(
                                            "TimeTrack Usage Warning",
                                            "You have 10 minutes left of usage for " + program + " in the " + block.Name + " block."
                                        );
                                        break;
                                    default:
                                        break;
                                }
                                // Checks if the usage of the individual program is greater than the limit
                                if (blockDict[program] > usageLimit)
                                {
                                    // Terminates the program if the usage is greater than the limit
                                    TerminateProgramByPID(pid);
                                    // Shows a warning notification
                                    ShowWarningNotification(
                                        "TimeTrack Usage Warning",
                                        "You have reached the usage limit for " + program + " in the " + block.Name + " block."
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // Function that will show a warning notification
    public static void ShowWarningNotification(string title, string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ((MainWindow)System.Windows.Application.Current.MainWindow).MainWindowShowWarningNotification(title, message);
        });
    }

    // Function that will terminate a program by it's process identifier (PID)
    public static void TerminateProgramByPID(int pid)
    {
        try
        {
            // Gets the user settings to see what type of block the user wants
            Settings userSettings = SettingsModel.GetUserSettings();
            if (userSettings.BlockType == "Exit Program")
            {
                // Kills the program
                if (System.Diagnostics.Process.GetProcesses().Any(x => x.Id == pid))
                {
                    System.Diagnostics.Process.GetProcessById(pid).Kill();
                }
            }
            else if (userSettings.BlockType == "Minimise Window")
            {
                // Minimises the program
                Process process = Process.GetProcessById(pid);
                if (process != null)
                {
                    ShowWindowAsync(process.MainWindowHandle, SW_MINIMIZE);
                }
            }
        }
        // Catches any errors when trying to access or terminate the process
        // This is needed as some system apps are not allowed to be killed or accessed
        // It also prevents crashes when the PID is no longer existant (user closes program) in between 
        // the update current program function being triggered and the termination function is triggered
        // This gap is usually a couple milliseconds at most but still can occur. 
        catch (Exception e)
        {
            // Logs the error
            string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error terminating PID {pid} - {e}";
            using (StreamWriter writer = File.AppendText("user\\log.txt"))
            {
                writer.WriteLine(errorWithTimestamp);
            }
        }
    }
    
    // Function that deserialises the blockstatus.json file and returns the dictionary
    // This file contains information on the program usage for each program that is being monitored by a block 
    public static Dictionary<string, object> GetBlockStatus()
    {
        var blockStatusDict = JsonConvert.DeserializeObject<Dictionary<string,object>>(File.ReadAllText("user\\blockstatus.json"));
        return blockStatusDict;
    }

    // Function that writes the input block status dictionary to the blockstatus.json file
    public static void WriteBlockStatus(Dictionary<string, object> input)
    {
        using (StreamWriter writer = new StreamWriter("user\\blockstatus.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(input));
        } 
    }

    // Function that will update the usage of a program in the block status
    // The add parameter is needed as if the function is called from the block creation menu, that does not mean
    // the program is open and thus will add 0 seconds but just adds the program to the block status tracking. 
    // If the function is called form the update current program function, it will add a second instead. 
    public static void UpdateBlockStatus(Block block, string program, int add)
    {
        Dictionary<string, object> blockStatus = GetBlockStatus();
        if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)"))
        {
            // Checks if the block is already in the block status file
            if (blockStatus.ContainsKey(block.Name))
            {
                // Checks if the program is already in the block status
                Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                if (blockDict.ContainsKey(program))
                {
                    // Adds a second to the program usage time
                    blockDict[program] = blockDict[program] + add;
                }
                else
                {
                    // Calculates how much time has been used in the day and adds the program to the block status
                    blockDict[program] = ProgramUsageModel.GetProgramUsageSinceMidnight(program) + add;
                }
                blockStatus[block.Name] = blockDict;
            }
            else
            {
                // Calculates how much time that the program has been used in the day and adds the program and block to block status
                blockStatus[block.Name] = new Dictionary<string, int>{{program, ProgramUsageModel.GetProgramUsageSinceMidnight(program) + add}}; 
            }
            // Write the updated block status to the database
            WriteBlockStatus(blockStatus);
        }
        /* can parse more types of blocks here */
    }
    
    // Rename a block in the block status
    public static void RenameBlockStatus(string oldname, string newname)
    {
        // Gets the block status file
        Dictionary<string, object> blockstatus = GetBlockStatus();
        // Checks if the block is actually in the block status file
        if (blockstatus.ContainsKey(oldname))
        {
            // Get the value of the block in the block status file
            object value = blockstatus[oldname];
            // Remove the old block from the block status file and add the new block
            blockstatus.Remove(oldname);
            blockstatus.Add(newname, value);
            // Write the updated block status to the database
            WriteBlockStatus(blockstatus);
        }
    }
    
    // Function to delete a block from the block status
    public static void DeleteBlockStatus(string oldname)
    {
        // Gets the block status file
        Dictionary<string, object> blockstatus = GetBlockStatus();
        // Checks if the block is actually in the block status file
        if (blockstatus.ContainsKey(oldname))
        {
            // Remove the block from the block status file
            blockstatus.Remove(oldname);
            // Write the updated block status to the database
            WriteBlockStatus(blockstatus);
        }
    }
}

// Class that represents a block
public class Block : INotifyPropertyChanged
{
    public string Name { get; set; }
    public bool Status { get; set; }
    public string Type { get; set; }
    
    // Implement OnPropertyChanged of the programs list to allow for the edit block UI to update when the list is changed
    private List<string> _programs;
    public List<string> Programs
    {
        get { return _programs; }
        set
        {
            if (_programs != value)
            {
                _programs = value;
                OnPropertyChanged(nameof(Programs));
            }
        }
    }
    
    // Implement OnPropertyChanged of the conditions dictionary to allow for the edit block UI to update when the dictionary is changed
    private Dictionary<int, BlockCondition> _conditions;
    public Dictionary<int, BlockCondition> Conditions
    {
        get { return _conditions; }
        set
        {
            if (_conditions != value)
            {
                _conditions = value;
                OnPropertyChanged(nameof(Conditions));
            }
        }
    }
    
    // Initialise a new Block object with the default blank values
    public Block()
    {
        Name = string.Empty;
        Status = true;
        Type = "Usage Limit (Combined)";
        Programs = new List<string>();
        Conditions = new Dictionary<int, BlockCondition>{
            {1, new BlockCondition{
                Criteria = new List<string>{"0", "0"},
                TimeCriteria = new List<string>()
                }
            }
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Class that represents a block condition
public class BlockCondition
{
    // Criteria is a list of strings that represent the criteria for the condition
    // i.e. if the condition is 1 hour, 15 minutes, the criteria will be {"1", "15"}
    public List<string> Criteria { get; set; }

    // TimeCriteria is a list of strings that represent the time criteria for the condition
    // i.e. if the condition is Monday, Tuesday, Friday, the criteria will be {"Mon", "Tue", "Fri"}
    public List<string> TimeCriteria { get; set; }
}