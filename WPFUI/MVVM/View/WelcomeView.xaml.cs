using System.IO;
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
        if (this.Name != "")
        {
            SettingsModel.WriteSettings(new Settings{UserName = this.Name, SystemApps = false, BlockType = "exit"});
            Close();
        }
    }
}