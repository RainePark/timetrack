using System;
using System.Windows;
using System.Windows.Input;

namespace WPFUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
