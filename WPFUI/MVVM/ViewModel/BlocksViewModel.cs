using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;

namespace WPFUI.MVVM.ViewModel
{
    // In hindight, most of this code should be done in the View to create the UI
    class BlocksViewModel : IPage
    { 
        private string _pageTitle;   
        public string PageTitle
        {
            get => this._pageTitle;
            set 
            { 
                this._pageTitle = value; 
                OnPropertyChanged();
            }
        }
        
        // Define the stack panel that will contain all the blocks
        private StackPanel _blocksStackPanel;
        public StackPanel BlocksStackPanel
        {
            get => this._blocksStackPanel;
            set
            {
                this._blocksStackPanel = value;
                OnPropertyChanged();
            }
        }
        
        // Set up the commands for the buttons
        public ICommand BlockToggleCommand { get; set; }
        public ICommand NewBlockCommand { get; set; }
        public ICommand EditBlockCommand { get; set; }
        public ICommand RefreshBlocksCommand { get; set; }

        public BlocksViewModel()
        {
            this.PageTitle = "Blocks";
            // Create the stack panel that will contain all the blocks when the ViewModel is launched
            RefreshBlocksPage();
            BlockToggleCommand = new RelayCommand(BlockToggleButton_Clicked);
            NewBlockCommand = new RelayCommand(CreateNewBlock_Clicked);
            EditBlockCommand = new RelayCommand(EditBlock_Clicked);
            RefreshBlocksCommand = new RelayCommand(RefreshBlocksCommand_Function);
        }

        // Get all the blocks from the model and create a stack panel with them that is updated in a property of the ViewModel
        public void RefreshBlocksPage()
        {
            var blockList = BlocksModel.GetAllBlocks();
            BlocksStackPanel = CreateBlockStackPanel(blockList);
        }
        
        private void RefreshBlocksCommand_Function(object parameter)
        {
            RefreshBlocksPage();
        }

        // Toggle the status of the block and update the blocks page
        private void BlockToggleButton_Clicked(object sender)
        {
            string blockName = ((ToggleButton)sender).Tag.ToString();
            Block block = BlocksModel.GetAllBlocks()[blockName];
            if (block != null)
            {
                block.Status = !block.Status;
                BlocksModel.UpdateBlock(blockName, block);
            }
            RefreshBlocksPage();
        }
        
        // Open a new window to create a new block
        private void CreateNewBlock_Clicked(object sender)
        {
            Block newBlock = new Block();
            EditBlockView editNewBlock = new EditBlockView(new EditBlockViewModel(new Block(), this, RefreshBlocksCommand));
            editNewBlock.ShowDialog();
        }
        
        // Open a new window to edit the block
        private void EditBlock_Clicked(object sender)
        {
            // Get the block name by accessing the Tag property of the button
            string blockName = ((Button)sender).Tag.ToString();
            // Get the block details and open the edit window
            Block block = BlocksModel.GetAllBlocks()[blockName];
            if (block != null)
            {
                EditBlockView editBlock = new EditBlockView(new EditBlockViewModel(block, this, RefreshBlocksCommand));
                editBlock.ShowDialog();
            }
        }

        // Create a stack panel that contains all the blocks
        public StackPanel CreateBlockStackPanel(Dictionary<string, Block> blockList)
        {
            List<string> keyList = blockList.Keys.ToList();
            // Order the list of blocks alphabetically
            keyList = keyList.OrderBy(key => key).ToList();
            StackPanel blockStackPanel = new StackPanel{Orientation = Orientation.Vertical};
            // Check if there are any blocks to display
            if (keyList.Count > 0)
            {
                // Loop through the list of blocks and create a block panel for each one
                for (int i = 0; i < keyList.Count; i++)
                {
                    Border newBlockPanel = CreateBlockPanel(blockList[keyList[i]], keyList[i]);
                    blockStackPanel.Children.Add(newBlockPanel);
                }
            }
            else
            {
                // Display a message if there are no blocks
                blockStackPanel.Children.Add(CreateNoBlocksText());
            }
            return blockStackPanel;
        }

        // Generate the TextBlock object that displays the message when there are no blocks
        public TextBlock CreateNoBlocksText()
        {
            TextBlock newTextBlock = new TextBlock{
                Text = "You have not created a block yet. Press the + to create one!",
                HorizontalAlignment = HorizontalAlignment.Center, 
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["SubheadingFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Regular, 
                Margin = new Thickness(0, 5, 0, 0)
            };
            return newTextBlock;
        }
        
        // Create a panel for an input block
        public Border CreateBlockPanel(Block block, string blockName)
        {
            // Create the main panel
            Border newBlockPanel = new Border{Width = 675, Height = 125, Margin = new Thickness(0, 0, 0, 15)};
            // Set the background to a gradient based on the status of the block
            if (block.Status) { newBlockPanel.Background = (LinearGradientBrush)Application.Current.Resources["GreenBlockGradientBrush"]; }
            else { newBlockPanel.Background = (LinearGradientBrush)Application.Current.Resources["RedBlockGradientBrush"]; }
            // Round the corners of the panel
            newBlockPanel.Clip = new RectangleGeometry{RadiusX = 10, RadiusY = 10, Rect = new Rect(0, 0, 675, 125)};
            // Define the grid that will contain the block details
            Grid newBlockPanelGrid = new Grid { Margin = new Thickness(10, 9, 10, 10) };
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(35)});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(25)});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(20)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(65)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(40)});
            // Create a stack panel with the list of programs in the block
            StackPanel newBlockPanelProgramList = CreateProgramListStackPanel(block.Programs);
            Grid.SetRow(newBlockPanelProgramList, 0);
            Grid.SetColumn(newBlockPanelProgramList, 0);
            Grid.SetColumnSpan(newBlockPanelProgramList, 3);
            newBlockPanelGrid.Children.Add(newBlockPanelProgramList);
            // Create the text block that displays the block name
            TextBlock newBlockPanelTitleText = new TextBlock {
                Text = blockName, 
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["SubheadingFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Regular, 
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetRow(newBlockPanelTitleText, 1);
            Grid.SetColumn(newBlockPanelTitleText, 0);
            Grid.SetColumnSpan(newBlockPanelTitleText, 2);
            newBlockPanelGrid.Children.Add(newBlockPanelTitleText);
            // Display the image of the block type
            Image blockTypeImage = new Image{HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 15, Stretch = Stretch.Uniform, Margin = new Thickness(0, 5, 0, 0)};
            if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)")) { blockTypeImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/stopwatch.ico")); }
            else { blockTypeImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/iconbackground-white.ico")); }
            Grid.SetRow(blockTypeImage, 2);
            Grid.SetColumn(blockTypeImage, 0);
            newBlockPanelGrid.Children.Add(blockTypeImage);
            // Display the block type and limit
            TextBlock blockTypeText = new TextBlock {
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["TextFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Light, 
                Margin = new Thickness(4, 5, 0, 0), 
                Text = $"{block.Type} - {block.Conditions[1].Criteria[0]} hour, {block.Conditions[1].Criteria[1]} minute limit"
            };
            Grid.SetRow(blockTypeText,2);
            Grid.SetColumn(blockTypeText, 1);
            newBlockPanelGrid.Children.Add(blockTypeText);
            // Display the clock icon for the block details
            Image blockDetailsImage = new Image{HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 15, Stretch = Stretch.Uniform, Margin = new Thickness(0, 4, 0, 0)};
            blockDetailsImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/clock.ico"));
            Grid.SetRow(blockDetailsImage, 3);
            Grid.SetColumn(blockDetailsImage, 0);
            newBlockPanelGrid.Children.Add(blockDetailsImage);
            // Display the block details
            TextBlock blockDetailsText = new TextBlock {
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["TextFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Light, 
                Margin = new Thickness(4, 5, 0, 0), 
                Text = string.Join(", ", block.Conditions[1].TimeCriteria)
            };
            Grid.SetRow(blockDetailsText, 3);
            Grid.SetColumn(blockDetailsText, 1);
            newBlockPanelGrid.Children.Add(blockDetailsText);
            // Display the button to edit the block
            Button blockDetailsExpandButton = new Button {
                Style = (Style)Application.Current.Resources["ExpandButtonTheme"], 
                Width = 31, 
                Height = 31, 
                BorderThickness = new Thickness(0), 
                HorizontalAlignment = HorizontalAlignment.Right, 
                Clip = new RectangleGeometry{RadiusX = 8, RadiusY = 8, Rect = new Rect(0, 0, 31, 31)}, 
                Tag = blockName
            };
            Grid.SetRow(blockDetailsExpandButton, 0);
            Grid.SetColumn(blockDetailsExpandButton, 3);
            newBlockPanelGrid.Children.Add(blockDetailsExpandButton);
            // Display the block status
            TextBlock blockStatusLabel = new TextBlock {
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["TextFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Light, 
                Margin = new Thickness(0, 5, 5, 0), 
                TextAlignment = TextAlignment.Right
            };
            if (block.Status) { blockStatusLabel.Text = "Enabled"; }
            else { blockStatusLabel.Text = "Disabled"; }
            Grid.SetRow(blockStatusLabel, 3);
            Grid.SetColumn(blockStatusLabel, 2);
            newBlockPanelGrid.Children.Add(blockStatusLabel);
            // Display the toggle button to change the block status
            ToggleButton blockStatusToggle = new ToggleButton {
                Style = (Style)Application.Current.Resources["ToggleButtonTheme"], 
                HorizontalAlignment = HorizontalAlignment.Right, 
                Margin = new Thickness(0, 4.5, 0, 0),
                Tag = blockName
            };
            if (block.Status) { blockStatusToggle.IsChecked = true; }
            else { blockStatusToggle.IsChecked = false; }
            Grid.SetRow(blockStatusToggle, 3);
            Grid.SetColumn(blockStatusToggle, 3);
            newBlockPanelGrid.Children.Add(blockStatusToggle);
            // Add the grid to the stack panel and return it
            newBlockPanel.Child = newBlockPanelGrid;
            return newBlockPanel;
        }

        // Create a stack panel with the list of programs that in a block
        public StackPanel CreateProgramListStackPanel(List<string> programs)
        {
            // Get the list of known programs
            var knownPrograms = ProgramUsageModel.GetKnownPrograms();
            StackPanel newStackPanel = new StackPanel {Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 5, 0)};
            // Check if the program list will overflow the block panel
            if (programs.Count < 15)
            {
                // Create an icon for each program in the list and add it to the stack panel
                for (int i = 0; i < programs.Count; i++)
                {
                    Grid newProgramIcon = CreateProgramIcon(knownPrograms[programs[i]].path);
                    newStackPanel.Children.Add(newProgramIcon);
                }
            }
            else
            {
                // Create an icon for each program in the list and add it to the stack panel
                for (int i = 0; i < 14; i++)
                {
                    Grid newProgramIcon = CreateProgramIcon(knownPrograms[programs[i]].path);
                    newStackPanel.Children.Add(newProgramIcon);
                }
                // Generate overflow icon that indicates there are additional programs
                Grid programOverflow = new Grid{Margin = new Thickness(0, 0, 5, 0)};
                programOverflow.RowDefinitions.Add(new RowDefinition{Height = new GridLength(33)});
                programOverflow.ColumnDefinitions.Add(new ColumnDefinition(){Width = new GridLength(33)});
                Border overflowBorder = new Border{Width = 31, Height = 31, BorderThickness = new Thickness(0)};
                overflowBorder.Clip = new RectangleGeometry{RadiusX = 8, RadiusY = 8, Rect = new Rect(0, 0, 31, 31)};
                BitmapImage overflowBitmap = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/app-overflow.ico"));
                overflowBorder.Background = new ImageBrush{Stretch = System.Windows.Media.Stretch.UniformToFill, ImageSource = overflowBitmap};
                programOverflow.Children.Add(overflowBorder);
                newStackPanel.Children.Add(programOverflow);
            }
            return newStackPanel;
        }
        
        // Create a grid with an icon for a program
        public Grid CreateProgramIcon(string programPath)
        {
            // Create the grid
            Grid newGrid = new Grid{Margin = new Thickness(0, 0, 5, 0)};
            newGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(33)});
            newGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(33)});
            // Set the background to white with rounded corners
            Border backgroundBorder = new Border{Width = 31, Height = 31, BorderThickness = new Thickness(0)};
            backgroundBorder.Clip = new RectangleGeometry{RadiusX = 8, RadiusY = 8, Rect = new Rect(0, 0, 31, 31)};
            BitmapImage backgroundBitmap = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/iconbackground-white.ico"));
            backgroundBorder.Background = new ImageBrush{Stretch = System.Windows.Media.Stretch.UniformToFill, ImageSource = backgroundBitmap};
            // Add the icon to the grid
            Border iconBorder = new Border{Width = 25, Height = 25, BorderThickness = new Thickness(0)};
            iconBorder.Background = new ImageBrush{Stretch = System.Windows.Media.Stretch.UniformToFill, ImageSource = GetExecutableIcon(programPath)};
            newGrid.Children.Add(backgroundBorder);
            newGrid.Children.Add(iconBorder);
            return newGrid;
        }
        
        // Get the icon of an executable from its path
        public static BitmapImage GetExecutableIcon(string exePath)
        {
            BitmapImage bitmapImage;
            try
            {
                // Extract the icon from the executable
                Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
                // Convert the icon to a bitmap source
                BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    exeIcon.Handle,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                // Convert the bitmap source to a bitmap image
                bitmapImage = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(stream);
                    stream.Position = 0;
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }
            }
            catch (Exception e)
            {
                // Logs the error
                string errorWithTimestamp = $"[E] {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error getting executable icon from {exePath} - {e}";
                using (StreamWriter writer = File.AppendText("user\\log.txt"))
                {
                    writer.WriteLine(errorWithTimestamp);
                }
                bitmapImage = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/iconbackground-white.ico"));
            }
            return bitmapImage;
        }

        // Function to create a bitmap image from a URI
        public static BitmapImage BitmapImageFromUri(Uri uri)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = uri;
            bitmapImage.EndInit();
            return bitmapImage;
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}