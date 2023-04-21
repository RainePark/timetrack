using System.IO;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject
    {
        public RelayCommand SettingsMenu1Command { get; set; }
        public RelayCommand SettingsMenu2Command { get; set; }
        public SettingsMenu1ViewModel SettingsMenu1VM { get; set; }
        public SettingsMenu2ViewModel SettingsMenu2VM { get; set; }

        private object _currentSettingsMenu;

        public object CurrentSettingsMenu
        {
            get { return _currentSettingsMenu; }
            set
            {
                _currentSettingsMenu = value;
                OnPropertyChanged();
            }
        }
        
        public SettingsViewModel()
        {
            SettingsMenu1VM = new SettingsMenu1ViewModel();
            SettingsMenu2VM = new SettingsMenu2ViewModel();

            CurrentSettingsMenu = SettingsMenu1VM;

            SettingsMenu1Command = new RelayCommand(o =>
            {
                CurrentSettingsMenu = SettingsMenu1VM;
            });
            SettingsMenu2Command = new RelayCommand(o =>
            {
                CurrentSettingsMenu = SettingsMenu2VM;
            });
        }
    }
}