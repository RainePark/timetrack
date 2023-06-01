using System;
using System.Drawing;
using System.IO;
using System.Threading;
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
        public TaskbarIcon taskbarIcon;
        private Mutex _mutex;
        public MainWindow()
        {
            InitializeComponent();
            
            // Create a TaskbarIcon control
            taskbarIcon = new TaskbarIcon();
            StreamResourceInfo streamResourceInfo = Application.GetResourceStream(new Uri("pack://application:,,,/Images/logo-ico.ico"));
            Stream stream = streamResourceInfo.Stream;
            taskbarIcon.Icon = new System.Drawing.Icon(stream);
            ContextMenu contextMenu = (ContextMenu)FindResource("TaskbarIconContextMenu");
            taskbarIcon.ContextMenu = contextMenu;
            taskbarIcon.ToolTipText = "TimeTrack";

            // Handle the TrayMouseDoubleClick event of the TaskbarIcon control
            taskbarIcon.TrayMouseDoubleClick += TaskbarIcon_TrayMouseDoubleClick;

            // Handle multiple instances of the executable being run using a Mutex
            bool createdNew;
            _mutex = new Mutex(true, "TimeTrack", out createdNew);

            // Check if the mutex was created successfully
            if (!createdNew)
            {
                // Show a warning if another instance of the program is already running and close the new instance of the program
                MessageBox.Show("Another instance of the program is already running. Try checking your taskbar and tray.");
                Close();
            }
        }
        
        // Relay a notification from the taskbar icon
        public void MainWindowShowWarningNotification(string title, string message)
        {
            taskbarIcon.ShowBalloonTip(title, message, BalloonIcon.Warning);
        }
        
        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // If the TaskbarIcon control is double-clicked, show the main window
            Show();
            WindowState = WindowState.Normal;
            // Ensure that the window is focused when opened from tray
            Activate();
            // Hide the taskbar icon when the program is opened
            // taskbarIcon.Visibility = Visibility.Collapsed;
        }

        // Minimise the window to the tray when the minimise button is clicked and make the taskbar icon visible
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Release the mutex when the program is finished
            _mutex.ReleaseMutex();
        }
    }
}
