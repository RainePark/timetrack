using System;
using System.Collections.Generic;
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
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;

namespace WPFUI.MVVM.ViewModel
{
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
        
        public ICommand BlockToggleCommand { get; set; }
        
        public BlocksViewModel()
        {
            this.PageTitle = "Blocks";
            /*
            List<string> templist = new List<string>{"rider64","firefox"};
            BlocksModel.CreateNewBlock("test", "usage-limit", templist, new List<BlockCondition>());
            */
            RefreshBlocksPage();
            /*set on property change and trigger a refresh when anything is updated*/
            BlockToggleCommand = new RelayCommand(BlockToggleButton_Clicked);
        }

        public void RefreshBlocksPage()
        {
            var blockList = BlocksModel.GetAllBlocks();
            BlocksStackPanel = CreateBlockStackPanel(blockList);
        }

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

        public StackPanel CreateBlockStackPanel(Dictionary<string, Block> blockList)
        {
            List<string> keyList = blockList.Keys.ToList();
            StackPanel blockStackPanel = new StackPanel{Orientation = Orientation.Vertical};
            for (int i = 0; i < keyList.Count; i++)
            {
                Border newBlockPanel = CreateBlockPanel(blockList[keyList[i]], keyList[i]);
                blockStackPanel.Children.Add(newBlockPanel);
            }
            return blockStackPanel;
        }
        
        public Border CreateBlockPanel(Block block, string blockName)
        {
            Border newBlockPanel = new Border{Width = 675, Height = 125, Margin = new Thickness(0, 0, 0, 15)};
            if (block.Status) { newBlockPanel.Background = (LinearGradientBrush)Application.Current.Resources["GreenBlockGradientBrush"]; }
            else { newBlockPanel.Background = (LinearGradientBrush)Application.Current.Resources["RedBlockGradientBrush"]; }
            newBlockPanel.Clip = new RectangleGeometry{RadiusX = 10, RadiusY = 10, Rect = new Rect(0, 0, 675, 125)};
            
            Grid newBlockPanelGrid = new Grid { Margin = new Thickness(10, 9, 10, 10) };
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(35)});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(25)});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto});
            newBlockPanelGrid.RowDefinitions.Add(new RowDefinition{Height = GridLength.Auto});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(20)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(1, GridUnitType.Star)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(65)});
            newBlockPanelGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(40)});
            
            StackPanel newBlockPanelProgramList = CreateProgramListStackPanel(block.Programs);
            Grid.SetRow(newBlockPanelProgramList, 0);
            Grid.SetColumn(newBlockPanelProgramList, 0);
            Grid.SetColumnSpan(newBlockPanelProgramList, 2);
            newBlockPanelGrid.Children.Add(newBlockPanelProgramList);

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
            
            Image blockTypeImage = new Image{HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 15, Stretch = Stretch.Uniform, Margin = new Thickness(0, 5, 0, 0)};
            if ((block.Type == "usage-limit-total") || (block.Type == "usage-limit-perapp")) { blockTypeImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/stopwatch.ico")); }
            else { blockTypeImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/iconbackground-white.ico")); }
            Grid.SetRow(blockTypeImage, 2);
            Grid.SetColumn(blockTypeImage, 0);
            newBlockPanelGrid.Children.Add(blockTypeImage);

            TextBlock blockTypeText = new TextBlock {
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["TextFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Light, 
                Margin = new Thickness(4, 5, 0, 0)
            };
            if (block.Type == "usage-limit") { blockTypeText.Text = "Usage Limit"; }
            else { blockTypeText.Text = "Block Type"; }
            Grid.SetRow(blockTypeText,2);
            Grid.SetColumn(blockTypeText, 1);
            newBlockPanelGrid.Children.Add(blockTypeText);
            
            Image blockDetailsImage = new Image{HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Height = 15, Stretch = Stretch.Uniform, Margin = new Thickness(0, 4, 0, 0)};
            blockDetailsImage.Source = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/clock.ico"));
            Grid.SetRow(blockDetailsImage, 3);
            Grid.SetColumn(blockDetailsImage, 0);
            newBlockPanelGrid.Children.Add(blockDetailsImage);

            TextBlock blockDetailsText = new TextBlock {
                Foreground = Brushes.White, 
                Background = Brushes.Transparent, 
                FontSize = (Double)Application.Current.Resources["TextFontSize"], 
                FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["MainFont"], 
                FontWeight = FontWeights.Light, 
                Margin = new Thickness(4, 5, 0, 0)
            };
            blockDetailsText.Text = "TEMPORARY";
            Grid.SetRow(blockDetailsText, 3);
            Grid.SetColumn(blockDetailsText, 1);
            newBlockPanelGrid.Children.Add(blockDetailsText);
            
            Button blockDetailsExpandButton = new Button {
                Style = (Style)Application.Current.Resources["ExpandButtonTheme"], 
                Width = 31, 
                Height = 31, 
                BorderThickness = new Thickness(0), 
                HorizontalAlignment = HorizontalAlignment.Right, 
                Clip = new RectangleGeometry{RadiusX = 8, RadiusY = 8, Rect = new Rect(0, 0, 31, 31)}
            };
            Grid.SetRow(blockDetailsExpandButton, 0);
            Grid.SetColumn(blockDetailsExpandButton, 3);
            newBlockPanelGrid.Children.Add(blockDetailsExpandButton);

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
            
            newBlockPanel.Child = newBlockPanelGrid;
            return newBlockPanel;
        }

        public StackPanel CreateProgramListStackPanel(List<string> programs)
        {
            var knownPrograms = ProgramUsageModel.GetKnownPrograms();
            StackPanel newStackPanel = new StackPanel {Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 5, 0)};
            if (programs.Count < 14)
            {
                for (int i = 0; i < programs.Count; i++)
                {
                    Grid newProgramIcon = CreateProgramIcon(knownPrograms[programs[i]].path);
                    newStackPanel.Children.Add(newProgramIcon);
                }
            }
            else
            {
                for (int i = 0; i < 13; i++)
                {
                    Grid newProgramIcon = CreateProgramIcon(knownPrograms[programs[i]].path);
                    newStackPanel.Children.Add(newProgramIcon);
                }
                /* Add more options button */
            }
            return newStackPanel;
        }
        
        public Grid CreateProgramIcon(string programPath)
        {
            Grid newGrid = new Grid{Margin = new Thickness(0, 0, 5, 0)};
            newGrid.RowDefinitions.Add(new RowDefinition{Height = new GridLength(33)});
            newGrid.ColumnDefinitions.Add(new ColumnDefinition{Width = new GridLength(33)});
            Border backgroundBorder = new Border{Width = 31, Height = 31, BorderThickness = new Thickness(0)};
            backgroundBorder.Clip = new RectangleGeometry{RadiusX = 8, RadiusY = 8, Rect = new Rect(0, 0, 31, 31)};
            BitmapImage backgroundBitmap = BitmapImageFromUri(new Uri("pack://application:,,,/TimeTrack;component/Images/iconbackground-white.ico"));
            backgroundBorder.Background = new ImageBrush{Stretch = System.Windows.Media.Stretch.UniformToFill, ImageSource = backgroundBitmap};
            Border iconBorder = new Border{Width = 25, Height = 25, BorderThickness = new Thickness(0)};
            iconBorder.Background = new ImageBrush{Stretch = System.Windows.Media.Stretch.UniformToFill, ImageSource = GetExecutableIcon(programPath)};
            newGrid.Children.Add(backgroundBorder);
            newGrid.Children.Add(iconBorder);
            return newGrid;
        }

        public BitmapImage GetExecutableIcon(string exePath)
        {
            Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);

            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                exeIcon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            BitmapImage bitmapImage = new BitmapImage();
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
            return bitmapImage;
        }

        public BitmapImage BitmapImageFromUri(Uri uri)
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