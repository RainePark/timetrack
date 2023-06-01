using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;

namespace WPFUI.MVVM.ViewModel
{
    public class EditBlockViewModel : INotifyPropertyChanged
    {
        // Allow the view to know where it was called from
        public object Parent { get; set; }

        // Update the block in a separate class to retain the original block if the user does not wish to save changes
        private UpdatedBlockData _UpdatedBlockData;
        public UpdatedBlockData UpdatedBlockData
        {
            get { return _UpdatedBlockData; }
            set
            {
                if (_UpdatedBlockData != value)
                {
                    _UpdatedBlockData = value;
                    OnPropertyChanged(nameof(UpdatedBlockData));
                }
            }
        }

        private List<string> _selectedTimeCriteria;
        public List<string> SelectedTimeCriteria
        {
            get { return _selectedTimeCriteria; }
            set
            {
                if (_selectedTimeCriteria != value)
                {
                    _selectedTimeCriteria = value;
                    OnPropertyChanged(nameof(SelectedTimeCriteria));
                }
            }
        }
        
        // Set up commands for the view to bind to
        public ICommand AddExecutablePathCommand { get; set; }
        public ICommand AddRecommendedExecutableCommand { get; set; }
        public ICommand RemoveExecutablePathCommand { get; set; }
        public ICommand BlockNameTextBoxUnfocused { get; set; } 
        public ICommand SaveBlockCommand { get; set; }
        public ICommand DeleteBlockCommand { get; set; }
        public ICommand RefreshBlocksCommand { get; set; }
        public ICommand BlockTypeChangedCommand { get; set; }

        public EditBlockViewModel(Block block, object parent, ICommand refreshBlocksCommand= null)
        {
            // Set up the block to be edited
            string originalBlockName = block.Name;
            UpdatedBlockData = new UpdatedBlockData(block, originalBlockName);
            BlockNameTextBoxUnfocused = new RelayCommand(BlockNameTextBox_Unfocused);
            SaveBlockCommand = new RelayCommand(SaveBlock_Click);
            DeleteBlockCommand = new RelayCommand(DeleteBlock_Click);
            AddExecutablePathCommand = new RelayCommand(AddExecutablePath);
            AddRecommendedExecutableCommand = new RelayCommand(AddRecommendedExecutable);
            RemoveExecutablePathCommand = new RelayCommand(RemoveExecutablePath);
            BlockTypeChangedCommand = new RelayCommand(BlockTypeChanged);
            Parent = parent;
            RefreshBlocksCommand = refreshBlocksCommand;
        }

        private void SaveBlock_Click(object parameter)
        {
            // Get the new block data from the view
            UpdatedBlockData UpdatedBlockData = (UpdatedBlockData)parameter;
            // Validate block is correctly input before adding to the database
            if (UpdatedBlockData.Block != null)
            {
                // Check if the block name is empty
                if (string.IsNullOrEmpty(UpdatedBlockData.Block.Name))
                {
                    MessageBox.Show("Block name cannot be empty");
                    return;
                }
                // Check if block name is already in use
                if (BlocksModel.GetAllBlocks().ContainsKey(UpdatedBlockData.Block.Name) && UpdatedBlockData.Block.Name != UpdatedBlockData.OriginalBlockName)
                {
                    MessageBox.Show("There is already a block with this name");
                    return;
                }
                // Check if block name is valid
                if (!Regex.IsMatch(UpdatedBlockData.Block.Name, @"[a-zA-Z]"))
                {
                    MessageBox.Show("Block name must contain at least one alphabetical character");
                    return;
                }
                if (!Regex.IsMatch(UpdatedBlockData.Block.Name, @"^(?=.*\S)[a-zA-Z0-9 ]+$"))
                {
                    MessageBox.Show("Block name can only contain alphanumeric characters");
                    return;
                }
                // Check if block contains any programs
                if (UpdatedBlockData.Block.Programs.Count() < 1)
                {
                    MessageBox.Show("Block must contain at least 1 application");
                    return;
                }
                // Set block criterias to 0 if empty
                if (String.IsNullOrEmpty(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()))
                {
                    UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0] = "0";
                }
                if (String.IsNullOrEmpty(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()))
                {
                    UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1] = "0";
                }
                // Check if the block limit is less than or equal to 24 hours
                if (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) > 23)
                {
                    if ((Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) != 24) && (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) != 0))
                    {
                        MessageBox.Show("There is only 24 hours in a day");
                        return;
                    }
                }
                // Check if the minutes of the block limit is less than 60
                if (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) > 59)
                {
                    // Add an hour to the block limit if the minutes field is 60 and reset the minutes field to 0
                    if ((Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) == 60) && Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) <= 23)
                    {
                        UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0] = (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) + 1).ToString();
                        UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1] = "0";
                    }
                    else
                    {
                        MessageBox.Show("There is only 60 minutes in an hour");
                        return;
                    }
                }
                // Check if the block limit is less than 0
                if ((Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) < 0) || (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) < 0))
                {
                    MessageBox.Show("You cannot have negative time!");
                    return;
                }
                // Make sure block is active for at least 1 day
                try
                {
                    if (SelectedTimeCriteria.Count == 0)
                    {
                        MessageBox.Show("You must select at least 1 day that the block is active");
                        return;
                    }
                }
                catch
                {
                    MessageBox.Show("You must select at least 1 day that the block is active");
                    return;
                }
                
                // Convert string of TextBox to an integer to remove trailing zeroes and then back to string for storage
                UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0] = Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()).ToString();
                UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1] = Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()).ToString();

                // Order the days of the week in the correct order
                UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].TimeCriteria = SelectedTimeCriteria.OrderBy(day => 
                {
                    switch (day)
                    {
                        case "Mon":
                            return 0;
                        case "Tue":
                            return 1;
                        case "Wed":
                            return 2;
                        case "Thu":
                            return 3;
                        case "Fri":
                            return 4;
                        case "Sat":
                            return 5;
                        case "Sun":
                            return 6;
                        default:
                            return 7;
                    }
                }).ToList();
                
                // Delete old version of block and append the updated one to the database
                if (UpdatedBlockData.OriginalBlockName != null)
                {
                    BlocksModel.DeleteBlock(UpdatedBlockData.OriginalBlockName);
                }
                BlocksModel.AppendBlockToDatabase(UpdatedBlockData.Block);
                BlocksModel.RenameBlockStatus(UpdatedBlockData.OriginalBlockName, UpdatedBlockData.Block.Name);

                // Update the block status of each program in the block without adding an extra second of usage
                foreach (string processName in UpdatedBlockData.Block.Programs)
                {
                    BlocksModel.UpdateBlockStatus(UpdatedBlockData.Block, processName, 0);
                }
            }
            // Refresh the blocks list if the window is opened from the blocks tab
            if (Parent is BlocksViewModel)
            {
                if (RefreshBlocksCommand != null)
                {
                    RefreshBlocksCommand.Execute(null);
                }
            }
            CloseWindow();
        }

        private void DeleteBlock_Click(object parameter)
        {
            // Confirm that the user wants to delete the block
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this block?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            UpdatedBlockData UpdatedBlockData = (UpdatedBlockData)parameter;
            // Delete the block from the databases
            if (UpdatedBlockData.OriginalBlockName != null)
            {
                if (UpdatedBlockData.Block != null)
                {
                    BlocksModel.DeleteBlock(UpdatedBlockData.OriginalBlockName);
                    BlocksModel.DeleteBlockStatus(UpdatedBlockData.OriginalBlockName);
                }
            }
            // Refresh the blocks list if the window is opened from the blocks tab
            if (Parent is BlocksViewModel)
            {
                if (RefreshBlocksCommand != null)
                {
                    RefreshBlocksCommand.Execute(null);
                }
            }
            CloseWindow();
        }

        public void AddExecutablePath(object parameter)
        {
            // Open a file explorer to select an executable file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe";
            openFileDialog.Title = "Select an executable file";
            if (openFileDialog.ShowDialog() == true)
            {
                // Get the path and name of the executable file
                string programPath = openFileDialog.FileName;
                string processName = Path.GetFileNameWithoutExtension(programPath);
                
                /* Was going to create an instance of the process to get the process name however it did
                 create some lag when adding programs to a block and was unncessary as I have not found 
                 an instance where the process name is not the file name anyways. */
                //string processName = GetProcessName(programPath);
                
                // Stop the user from adding TimeTrack or a system app as a blocked application
                ProgramUsageModel.AppendProgramDetails(programPath, processName);
                if (processName == "TimeTrack")
                {
                    MessageBox.Show("TimeTrack cannot be added as a blocked application.");
                    return;
                }
                if (ProgramUsageModel.GetKnownPrograms()[processName].system)
                {
                    MessageBox.Show("System apps cannot be added as blocked application.");
                    return;
                }
                // Stop the user from adding the same application twice
                if (UpdatedBlockData.Block.Programs.Contains(processName))
                {
                    MessageBox.Show("This block already contains this application.");
                    return;
                }
                // Add the application to the block
                UpdatedBlockData.Block.Programs.Add(processName);
                UpdatedBlockData.Programs = new ObservableCollection<string>(UpdatedBlockData.Block.Programs);
            }
        }

        public void AddRecommendedExecutable(object parameter)
        {
            // Get the process name of the recommended application
            string processName = (string)parameter;
                
            // Stop the user from adding TimeTrack or a system app as a blocked application
            if (processName == "TimeTrack")
            {
                MessageBox.Show("TimeTrack cannot be added as a blocked application.");
                return;
            }
            if (ProgramUsageModel.GetKnownPrograms()[processName].system)
            {
                MessageBox.Show("System apps cannot be added as blocked application.");
                return;
            }
            // Stop the user from adding the same application twice
            if (UpdatedBlockData.Block.Programs.Contains(processName))
            {
                MessageBox.Show("This block already contains this application.");
                return;
            }
            // Add the application to the block
            UpdatedBlockData.Block.Programs.Add(processName);
            UpdatedBlockData.Programs = new ObservableCollection<string>(UpdatedBlockData.Block.Programs);
        }
        
        string GetProcessName(string executablePath)
        {
            // Start the process
            ProcessStartInfo startInfo = new ProcessStartInfo(executablePath);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // Wait for the process to start
            Thread.Sleep(200);

            // Loop until the process name is available
            string processName = null;
            while (string.IsNullOrEmpty(processName))
            {
                processName = process.ProcessName;
                Thread.Sleep(100);
            }

            // Wait for a few seconds
            Thread.Sleep(200);

            // Close the process
            process.Kill();

            // Return the process name
            return processName;
        }

        public void RemoveExecutablePath(object parameter)
        {
            // Remove the selected application from the block
            string processName = (string)parameter;
            if (UpdatedBlockData.Block.Programs.Contains(processName))
            {
                UpdatedBlockData.Block.Programs.Remove(processName);
            }
            UpdatedBlockData.Programs = new ObservableCollection<string>(UpdatedBlockData.Block.Programs);
            // Remove the application from the block status
            Dictionary<string, object> newBlockStatus = BlocksModel.GetBlockStatus();
            if ((UpdatedBlockData.Block.Type == "Usage Limit (Combined)") || (UpdatedBlockData.Block.Type == "Usage Limit (Per App)"))
            {
                if (newBlockStatus.ContainsKey(UpdatedBlockData.Block.Name))
                {
                    Dictionary<string, int> blockDict = ((JObject)newBlockStatus[UpdatedBlockData.Block.Name]).ToObject<Dictionary<string, int>>();
                    blockDict.Remove(processName);
                    newBlockStatus[UpdatedBlockData.Block.Name] = blockDict;
                }
                BlocksModel.WriteBlockStatus(newBlockStatus);
            }
        }
        
        public void BlockTypeChanged(object parameter)
        {
            // Reset the block conditions if the block type is changed
            UpdatedBlockData.Conditions.Clear();
            if (UpdatedBlockData.Type == "Usage Limit (Combined)" || UpdatedBlockData.Type == "Usage Limit (Per App)")
            {
                UpdatedBlockData.Block.Conditions = new Dictionary<int, BlockCondition>{
                    {1, new BlockCondition{
                            Criteria = new List<string>{"0", "0"},
                            TimeCriteria = new List<string>()
                        }
                    }
                };
                UpdatedBlockData.Conditions = new ObservableCollection<KeyValuePair<int, BlockCondition>>(UpdatedBlockData.Block.Conditions);
            }
        }

        private void BlockNameTextBox_Unfocused(object parameter)
        {
            // Remove any whitespace from the block name
            UpdatedBlockData.Block.Name = UpdatedBlockData.Block.Name.Trim();
        }

        // Close the window of the edit block window
        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class UpdatedBlockData : INotifyPropertyChanged
    {
        private Block _block;
        public Block Block
        {
            get { return _block; }
            set
            {
                if (_block != value)
                {
                    _block = value;
                    OnPropertyChanged(nameof(Block));
                }
            }
        }

        // Create an observable collection of the block programs to be displayed in the application list
        private ObservableCollection<string> _programs;
        public ObservableCollection<string> Programs
        {
            get { return _programs; }
            set
            {
                _programs = value;
                OnPropertyChanged(nameof(Programs));
            }
        }
        
        // Create an observable collection of recommended applications to block
        public ObservableCollection<string> RecommendedBlockApplications { get; set; }

        // Update the block type of the block object when the type is changed
        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;
                this.Block.Type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
        
        // Update the block conditions of the block object when the conditions are changed
        private ObservableCollection<KeyValuePair<int, BlockCondition>> _conditions;
        public ObservableCollection<KeyValuePair<int, BlockCondition>> Conditions
        {
            get { return _conditions; }
            set
            {
                _conditions = value;
                this.Block.Conditions = value.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                OnPropertyChanged(nameof(Conditions));
            }
        }
        
        public string OriginalBlockName;

        public ObservableCollection<string> BlockTypeOptions { get; set; }

        public UpdatedBlockData(Block block, string originalBlockName)
        {
            // Set the block data to be shown in the edit block window
            // Uses default values if it is a new block with no data
            Block = block;
            try { Programs = new ObservableCollection<string>(Block.Programs); }
            catch { Programs = new ObservableCollection<string>(); }
            RecommendedBlockApplications = GetRecommendedBlocks(block.Programs);
            try { Type = Block.Type; }
            catch { Type = "Usage Limit (Combined)"; }
            Conditions = new ObservableCollection<KeyValuePair<int, BlockCondition>>(Block.Conditions);
            OriginalBlockName = originalBlockName;
            BlockTypeOptions = new ObservableCollection<string>
            {
                "Usage Limit (Combined)",
                "Usage Limit (Per App)"
            };
        }
        
        public ObservableCollection<string> GetRecommendedBlocks(List<string> currentApplications)
        {
            // Get dictionary of most used programs
            Dictionary<string, int> mostUsedDictionary = UsageViewModel.GetProgramUsageDictionary();
            ObservableCollection<string> outputCollection = new ObservableCollection<string>();
            // Loop through and check that none of the programs recommended are already in the block
            List<string> keysToRemove = new List<string>();
            foreach (string key in mostUsedDictionary.Keys)
            {
                if (currentApplications.Contains(key))
                {
                    keysToRemove.Add(key);
                }
            }
            // Remove dupliate programs
            foreach (string key in keysToRemove)
            {
                mostUsedDictionary.Remove(key);
            }
            // Remove TimeTrack from recommended programs
            if (mostUsedDictionary.Keys.Contains("TimeTrack"))
            {
                mostUsedDictionary.Remove("TimeTrack");
            }
            // Get the top 6 most used programs and add them to the outputCollection for display as a recommendation
            Dictionary<string, int> mostUsedDictionaryTrimmed = mostUsedDictionary.Take(6).ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (KeyValuePair<string, int> pair in mostUsedDictionaryTrimmed)
            {
                outputCollection.Add(pair.Key);
            }
            return outputCollection;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}