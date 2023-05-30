using System;
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
        private TaskbarIcon taskbarIcon;
        private Mutex _mutex;
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

            // Handle multiple instances of the executable being run using a Mutex
            bool createdNew;
            _mutex = new Mutex(true, "TimeTrack", out createdNew);

            // Check if the mutex was created successfully
            if (!createdNew)
            {
                // Another instance of the program is already running
                MessageBox.Show("Another instance of the program is already running. Try checking your taskbar and tray.");
                Close();
            }
        }

        private void TaskbarIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            // If the TaskbarIcon control is double-clicked, show the main window
            Show();
            WindowState = WindowState.Normal;
            Activate();
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Release the mutex when the program is finished
            _mutex.ReleaseMutex();
        }
    }
}
