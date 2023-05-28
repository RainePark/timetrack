using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;

namespace WPFUI.MVVM.View;

public partial class WelcomeView : Window
{
    public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(WelcomeView), new PropertyMetadata(string.Empty));
    public ICommand StartProgram { get; set; }
    
    public string Name
    {
        get { return (string)GetValue(NameProperty); }
        set { SetValue(NameProperty, value); }
    }
    public WelcomeView()
    {
        InitializeComponent();
        StartProgram = new RelayCommand(InitializeSettings);
        DataContext = this;
    }
    
    private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }

    public void InitializeSettings(object parameter)
    {
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
        SettingsModel.WriteSettings(new Settings{UserName = this.Name, SystemApps = false, BlockType = "Exit Program"});
        Close();
    }
}