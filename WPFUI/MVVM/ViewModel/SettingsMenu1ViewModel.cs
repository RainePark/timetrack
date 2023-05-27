using System.IO;
using WPFUI.MVVM.Model;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel;

public class SettingsMenu1ViewModel : ObservableObject
{
    private Settings _userSettings;
    public Settings UserSettings
    {
        get => _userSettings;
        set
        {
            _userSettings = value;
            OnPropertyChanged();
        }
    }

    public SettingsMenu1ViewModel(Settings userSettings)
    {
        UserSettings = userSettings;
        using (StreamWriter writer = new StreamWriter("user\\test.txt"))
        {
            writer.WriteLine(UserSettings.UserName);
        }
    }
}