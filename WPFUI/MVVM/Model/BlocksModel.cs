using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFUI.Core;

namespace WPFUI.MVVM.Model;

public class BlocksModel : ObservableObject
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    private const int SW_MINIMIZE = 6;
    
    public BlocksModel()
    {
        
    }
    
    public static Dictionary<string,Block> GetAllBlocks()
    {
        var blockList = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        return (blockList);
    }

    public static List<Block> GetAllActiveBlocks()
    {
        var fullBlockList = GetAllBlocks();
        List<Block> activeBlocks = new List<Block>();
        foreach (Block block in fullBlockList.Values)
        {
            if (block.Status)
            {
                if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)"))
                {
                    if (block.Conditions[1].TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                    {
                        activeBlocks.Add(block);
                    }
                }
            }
        }
        return activeBlocks;
    }

    public static Block CreateNewBlock(string name, string type, List<string> programs)
    {
        return new Block { Name = name, Status = true, Type = type, Programs = programs, Conditions = new Dictionary<int, BlockCondition>()};
    }
    
    public static void AppendBlockToDatabase(Block block)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[block.Name] = block;
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    public static Dictionary<int, BlockCondition> CreateNewBlockCondition(string blocktype, Dictionary<int, BlockCondition> blockConditions, List<string> criteria, List<string> timecriteria)
    {
        List<int> blockConditionKeys = blockConditions.Keys.ToList();
        int newBlockConditionID;
        if (blockConditionKeys.Count > 0)
        {
            newBlockConditionID = blockConditionKeys.Max() + 1;
        }
        else
        {
            newBlockConditionID = 1;
        }
        BlockCondition newBlockCondition = new BlockCondition{ Criteria = criteria, TimeCriteria = timecriteria };
        blockConditions[newBlockConditionID] = newBlockCondition;
        return blockConditions;
    }

    public static void UpdateBlock(string blockName, Block block)
    {
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks[blockName] = block;
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    public static void DeleteBlock(string name){
        var existingBlocks = JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
        existingBlocks.Remove(name);
        using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(existingBlocks));
        }
    }

    public static void CheckAllBlocks(string program, int pid)
    {
        var blockList = GetAllBlocks();
        foreach (Block block in blockList.Values)
        {
            if (block.Programs.Contains(program))
            {
                UpdateBlockStatus(block, program, 1);
            }
        }

        // Go through each block again and check if the current program needs to be blocked
        foreach (Block block in blockList.Values)
        {
            if (block.Status)
            {
                if (block.Programs.Contains(program))
                {
                    Dictionary<string, object> blockStatus = GetBlockStatus();
                    if (block.Type == "Usage Limit (Combined)")
                    {
                        Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                        int totalUsage = 0;
                        foreach (int value in blockDict.Values)
                        {
                            totalUsage += value;
                        }
                        foreach (BlockCondition blockCondition in block.Conditions.Values)
                        {
                            if (blockCondition.TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                            {
                                if (totalUsage > ((Convert.ToInt32(blockCondition.Criteria[0])*60*60)+(Convert.ToInt32(blockCondition.Criteria[1])*60)))
                                {
                                    TerminateProgramByPID(pid);
                                }
                            }
                        }
                    }
                    else if (block.Type == "Usage Limit (Per App)")
                    {
                        Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                        foreach (BlockCondition blockCondition in block.Conditions.Values)
                        {
                            if (blockCondition.TimeCriteria.Contains(DateTime.Now.ToString("dddd").Substring(0, 3)))
                            {
                                if (blockDict[program] > ((Convert.ToInt32(blockCondition.Criteria[0])*60*60)+(Convert.ToInt32(blockCondition.Criteria[1])*60)))
                                {
                                    TerminateProgramByPID(pid);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static void TerminateProgramByPID(int pid)
    {
        try
        {
            Settings userSettings = SettingsModel.GetUserSettings();
            if (userSettings.BlockType == "Exit Program")
            {
                if (System.Diagnostics.Process.GetProcesses().Any(x => x.Id == pid))
                {
                    System.Diagnostics.Process.GetProcessById(pid).Kill();
                }
            }
            else if (userSettings.BlockType == "Minimise Window")
            {
                Process process = Process.GetProcessById(pid);
                if (process != null)
                {
                    ShowWindowAsync(process.MainWindowHandle, SW_MINIMIZE);
                }
            }
        }
        catch (Exception e)
        {
            string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error terminating PID {pid} - {e}";
            using (StreamWriter writer = File.AppendText("user\\log.txt"))
            {
                writer.WriteLine(errorWithTimestamp);
            }
        }
    }

    public static Dictionary<string, object> GetBlockStatus()
    {
        var blockStatusDict = JsonConvert.DeserializeObject<Dictionary<string,object>>(File.ReadAllText("user\\blockstatus.json"));
        return blockStatusDict;
    }

    public static void WriteBlockStatus(Dictionary<string, object> input)
    {
        using (StreamWriter writer = new StreamWriter("user\\blockstatus.json"))
        {
            writer.WriteLine(JsonConvert.SerializeObject(input));
        } 
    }

    public static void UpdateBlockStatus(Block block, string program, int add)
    {
        Dictionary<string, object> blockStatus = GetBlockStatus();
        if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)"))
        {
            if (blockStatus.ContainsKey(block.Name))
            {
                Dictionary<string, int> blockDict = ((JObject)blockStatus[block.Name]).ToObject<Dictionary<string, int>>();
                if (blockDict.ContainsKey(program))
                {
                    blockDict[program] = blockDict[program] + add;
                }
                else
                {
                    blockDict[program] = ProgramUsageModel.GetProgramUsageSinceMidnight(program) + add;
                }
                blockStatus[block.Name] = blockDict;
            }
            else
            {
                blockStatus[block.Name] = new Dictionary<string, int>{{program, ProgramUsageModel.GetProgramUsageSinceMidnight(program) + add}}; 
            }
            WriteBlockStatus(blockStatus);
        }
        /*parse more conditions here later*/
    }
    
    public static void RenameBlockStatus(string oldname, string newname)
    {
        Dictionary<string, object> blockstatus = GetBlockStatus();
        if (blockstatus.ContainsKey(oldname))
        {
            object value = blockstatus[oldname];
            blockstatus.Remove(oldname);
            blockstatus.Add(newname, value);
            WriteBlockStatus(blockstatus);
        }
    }
    
    public static void DeleteBlockStatus(string oldname)
    {
        Dictionary<string, object> blockstatus = GetBlockStatus();
        if (blockstatus.ContainsKey(oldname))
        {
            object value = blockstatus[oldname];
            blockstatus.Remove(oldname);
            WriteBlockStatus(blockstatus);
        }
    }
}

public class Block : INotifyPropertyChanged
{
    public string Name { get; set; }
    public bool Status { get; set; }
    public string Type { get; set; }
    
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

public class BlockCondition
{
    public List<string> Criteria { get; set; }
    public List<string> TimeCriteria { get; set; }
}