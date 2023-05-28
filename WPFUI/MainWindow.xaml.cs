using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Resources;
using Hardcodet.Wpf.TaskbarNotification;
using WPFUI.MVVM.Model;

namespace WPFUI
{
    public partial class MainWindow : Window
    {
        private TaskbarIcon taskbarIcon;
        public MainWindow()
        {
            InitializeComponent();
            
            // Create a TaskbarIcon control
            taskbarIcon = new TaskbarIcon();
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Images/logo-ico.ico"));
            Stream stream = streamResourceInfo.Stream;
            taskbarIcon.Icon = new System.Drawing.Icon(stream);
            taskbarIcon.Visibility = Visibility.Collapsed;
            ContextMenu contextMenu = (ContextMenu)FindResource("TaskbarIconContextMenu");
            taskbarIcon.ContextMenu = contextMenu;
            taskbarIcon.ToolTipText = "TimeTrack";

            // Handle the TrayMouseDoubleClick event of the TaskbarIcon control
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // If the TaskbarIcon control is double-clicked, show the main window
            Show();
            WindowState = WindowState.Normal;
            taskbarIcon.Visibility = Visibility.Collapsed;
        }

        private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
        {
            Hide();
            taskbarIcon.Visibility = Visibility.Visible;
        }

        private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ExitProgram(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
