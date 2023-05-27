using WPFUI.MVVM.Model;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel;

public class SettingsMenu2ViewModel : ObservableObject
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

    public SettingsMenu2ViewModel(Settings userSettings)
    {
        UserSettings = userSettings;
    }
}