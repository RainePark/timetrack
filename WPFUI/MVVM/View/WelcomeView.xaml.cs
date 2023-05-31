using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;

namespace WPFUI.MVVM.View;

public partial class WelcomeView : Window
{
    public ICommand StartProgram { get; set; }

    // Implement name property to bind to the textbox using a DependencyProperty so I don't need to implement INotifyPropertyChanged
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(WelcomeView), new PropertyMetadata(string.Empty));
    public string Name
    {
        get { return (string)GetValue(NameProperty); }
        set { SetValue(NameProperty, value); }
    }

    public WelcomeView()
    {
        InitializeComponent();
        // Initialize the command to start the program
        StartProgram = new RelayCommand(InitializeSettings);
        // Set the datacontext to the view code as there is no viewmodel
        DataContext = this;
    }

    // Close the Welcome menu and continue to the main program
    public void InitializeSettings(object parameter)
    {
        // Check if the name is valid
        if (string.IsNullOrEmpty(this.Name))
        {
            MessageBox.Show("Name cannot be empty");
            return;
        }
        Regex regex1 = new Regex(@"[a-zA-Z]");
        if (!regex1.IsMatch(this.Name))
        {
            MessageBox.Show("Name must contain at least one alphabetical character");
            return;
        }
        Regex regex2 = new Regex(@"^[a-zA-Z0-9]+$");
        if (!regex2.IsMatch(this.Name))
        {
            MessageBox.Show("Name can only contain alphanumeric characters and cannot contain spaces");
            return;
        }
        // Write the settings file based on default values and specified name
        SettingsModel.WriteSettings(new Settings{UserName = this.Name, SystemApps = false, BlockType = "Exit Program"});
        // Close the window to allow the main window to start
        Close();
    }

    // Allow window to be moved by dragging the title bar
    private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }

    // Close the window when the close button is clicked
    private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }    
}