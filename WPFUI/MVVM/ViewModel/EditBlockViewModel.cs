using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        public object Parent { get; set; }

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

        public ICommand AddExecutablePathCommand { get; set; }
        public ICommand RemoveExecutablePathCommand { get; set; }
        public ICommand BlockNameTextBoxUnfocused { get; set; } 
        public ICommand SaveBlockCommand { get; set; }
        public ICommand DeleteBlockCommand { get; set; }
        public ICommand RefreshBlocksCommand { get; set; }
        public ICommand BlockTypeChangedCommand { get; set; }

        public EditBlockViewModel(Block block, object parent, ICommand refreshBlocksCommand= null)
        {
            string originalBlockName = block.Name;
            UpdatedBlockData = new UpdatedBlockData(block, originalBlockName);
            BlockNameTextBoxUnfocused = new RelayCommand(BlockNameTextBox_Unfocused);
            SaveBlockCommand = new RelayCommand(SaveBlock_Click);
            DeleteBlockCommand = new RelayCommand(DeleteBlock_Click);
            AddExecutablePathCommand = new RelayCommand(AddExecutablePath);
            RemoveExecutablePathCommand = new RelayCommand(RemoveExecutablePath);
            BlockTypeChangedCommand = new RelayCommand(BlockTypeChanged);
            Parent = parent;
            RefreshBlocksCommand = refreshBlocksCommand;
        }

        private void SaveBlock_Click(object parameter)
        {
            UpdatedBlockData UpdatedBlockData = (UpdatedBlockData)parameter;
            if (UpdatedBlockData.Block != null)
            {
                // Validate block is correctly input before adding to the database
                if (string.IsNullOrEmpty(UpdatedBlockData.Block.Name))
                {
                    MessageBox.Show("Block name cannot be empty");
                    return;
                }
                if (!Regex.IsMatch(UpdatedBlockData.Block.Name, @"[a-zA-Z]"))
                {
                    MessageBox.Show("Block name must contain at least one alphabetical character");
                    return;
                }
                if (BlocksModel.GetAllBlocks().ContainsKey(UpdatedBlockData.Block.Name) && UpdatedBlockData.Block.Name != UpdatedBlockData.OriginalBlockName)
                {
                    MessageBox.Show("There is already a block with this name");
                    return;
                }
                if (!Regex.IsMatch(UpdatedBlockData.Block.Name, @"^(?=.*\S)[a-zA-Z0-9 ]+$"))
                {
                    MessageBox.Show("Block name can only contain alphanumeric characters");
                    return;
                }
                if (UpdatedBlockData.Block.Programs.Count() < 1)
                {
                    MessageBox.Show("Block must contain at least 1 application");
                    return;
                }
                if (String.IsNullOrEmpty(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()))
                {
                    UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0] = "0";
                }
                if (String.IsNullOrEmpty(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()))
                {
                    UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1] = "0";
                }
                if (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) > 23)
                {
                    if ((Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) != 24) && (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) != 0))
                    {
                        MessageBox.Show("There is only 24 hours in a day");
                        return;
                    }
                }
                if (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) > 59)
                {
                    MessageBox.Show("There is only 60 minutes in an hour");
                    return;
                }
                if ((Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[0].ToString()) < 0) || (Convert.ToInt32(UpdatedBlockData.Block.Conditions[Convert.ToInt32("1")].Criteria[1].ToString()) < 0))
                {
                    MessageBox.Show("You cannot have negative time!");
                    return;
                }
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

                foreach (string processName in UpdatedBlockData.Block.Programs)
                {
                    BlocksModel.UpdateBlockStatus(UpdatedBlockData.Block, processName, 0);
                }
            }
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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this block?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }
            UpdatedBlockData UpdatedBlockData = (UpdatedBlockData)parameter;
            if (UpdatedBlockData.OriginalBlockName != null)
            {
                if (UpdatedBlockData.Block != null)
                {
                    BlocksModel.DeleteBlock(UpdatedBlockData.OriginalBlockName);
                    BlocksModel.DeleteBlockStatus(UpdatedBlockData.OriginalBlockName);
                }
            }
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Executable files (*.exe)|*.exe";
            openFileDialog.Title = "Select an executable file";
            if (openFileDialog.ShowDialog() == true)
            {
                string programPath = openFileDialog.FileName;
                string processName = Path.GetFileNameWithoutExtension(programPath);
                
                /* Was going to create an instance of the process to get the process name however it did
                 create some lag when adding programs to a block and was unncessary as I have not found 
                 an instance where the process name is not the file name anyways. */
                //string processName = GetProcessName(programPath);
                
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
                if (UpdatedBlockData.Block.Programs.Contains(processName))
                {
                    MessageBox.Show("This block already contains this application.");
                    return;
                }
                UpdatedBlockData.Block.Programs.Add(processName);
                UpdatedBlockData.Programs = new ObservableCollection<string>(UpdatedBlockData.Block.Programs);
            }
        }
        
        string GetProcessName(string executablePath)
        {
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
            string processName = (string)parameter;
            if (UpdatedBlockData.Block.Programs.Contains(processName))
            {
                UpdatedBlockData.Block.Programs.Remove(processName);
            }
            UpdatedBlockData.Programs = new ObservableCollection<string>(UpdatedBlockData.Block.Programs);
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
            UpdatedBlockData.Block.Name = UpdatedBlockData.Block.Name.Trim();
        }

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
            Block = block;
            try { Programs = new ObservableCollection<string>(Block.Programs); }
            catch { Programs = new ObservableCollection<string>(); }
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}