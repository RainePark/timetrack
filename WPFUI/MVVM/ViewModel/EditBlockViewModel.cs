using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        public ICommand AddExecutablePathCommand { get; set; }
        public ICommand RemoveExecutablePathCommand { get; set; }
        public ICommand SaveBlockCommand { get; set; }
        public ICommand RefreshBlocksCommand { get; set; }

        public EditBlockViewModel(Block block, object parent, ICommand refreshBlocksCommand= null)
        {
            string originalBlockName = block.Name;
            UpdatedBlockData = new UpdatedBlockData { Block = block, OriginalBlockName = originalBlockName };
            SaveBlockCommand = new RelayCommand(SaveBlock_Click);
            RemoveExecutablePathCommand = new RelayCommand(RemoveExecutablePath);
            Parent = parent;
            RefreshBlocksCommand = refreshBlocksCommand;
        }

        private void SaveBlock_Click(object parameter)
        {
            UpdatedBlockData UpdatedBlockData = (UpdatedBlockData)parameter;
            if (UpdatedBlockData.Block != null)
            {
                if (UpdatedBlockData.OriginalBlockName != null)
                {
                    BlocksModel.DeleteBlock(UpdatedBlockData.OriginalBlockName);
                }

                // Validate block is correctly input before adding to the database
                if (string.IsNullOrEmpty(UpdatedBlockData.Block.Name))
                {
                    MessageBox.Show("Block name cannot be empty");
                    return;
                }
                if (BlocksModel.GetAllBlocks().ContainsKey(UpdatedBlockData.Block.Name) && UpdatedBlockData.Block.Name != UpdatedBlockData.OriginalBlockName)
                {
                    MessageBox.Show("There is already a block with this name already exists");
                    return;
                }

                if (!Regex.IsMatch(UpdatedBlockData.Block.Name, @"^[a-zA-Z0-9]+$"))
                {
                    MessageBox.Show("Block name can only contain ASCII characters and numbers");
                    return;
                }
            BlocksModel.AppendBlockToDatabase(UpdatedBlockData.Block);
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

        public void RemoveExecutablePath(object parameter)
        {
            string programPath = (string)parameter;
            if (UpdatedBlockData.Block.Programs.Contains(programPath))
            {
                UpdatedBlockData.Block.Programs.Remove(programPath);
            }
            UpdateItemsControl();
        }

        private void UpdateItemsControl(){
            var view = Application.Current.Windows.OfType<EditBlockView>().FirstOrDefault();
            var itemsControl = view?.FindName("EditBlockViewItemsControl") as ItemsControl;
            var bindingExpression = itemsControl?.GetBindingExpression(ItemsControl.ItemsSourceProperty);
            bindingExpression?.UpdateTarget();
            using (StreamWriter writer = new StreamWriter("user\\test.txt"))
            {
                var block = (Block)bindingExpression?.ResolvedSource;
                var programs = block?.Programs;
                if (programs != null)
                {
                    foreach (var program in programs)
                    {
                        writer.WriteLine(program);
                    }
                }
            }
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

        private string _originalBlockName;
        public string OriginalBlockName
        {
            get { return _originalBlockName; }
            set
            {
                if (_originalBlockName != value)
                {
                    _originalBlockName = value;
                    OnPropertyChanged(nameof(OriginalBlockName));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}