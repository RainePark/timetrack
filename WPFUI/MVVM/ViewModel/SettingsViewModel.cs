using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;

namespace WPFUI.MVVM.ViewModel
{
    class SettingsViewModel : IPage
    {         
        private Dictionary<PageName, object> SettingsPages { get; }
        
        private Settings _userSettings;   
        public Settings UserSettings
        {
            get => this._userSettings;
            set 
            { 
                this._userSettings = value; 
                OnPropertyChanged();
            }
        }


        private string _pageTitle;   
        public string PageTitle
        {
            get => this._pageTitle;
            set 
            { 
                this._pageTitle = value; 
                OnPropertyChanged();
            }
        }
        
        public ICommand SelectSettingsMenuCommand { get; set; }

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

        private Dictionary<string, object> _settingsMenus;

        public Dictionary<string, object> SettingsMenus
        {
            get { return _settingsMenus; }
            set
            {
                _settingsMenus = value;
                OnPropertyChanged();
            }
        }

        private bool _isUserSettingsSidebarRadioButtonChecked;
        public bool IsUserSettingsSidebarRadioButtonChecked
        {
            get { return _isUserSettingsSidebarRadioButtonChecked; }
            set
            {
                if (_isUserSettingsSidebarRadioButtonChecked != value)
                {
                    _isUserSettingsSidebarRadioButtonChecked = value;
                    OnPropertyChanged(nameof(IsUserSettingsSidebarRadioButtonChecked));
                }
            }
        }

        private bool _isBlocksSettingsSidebarRadioButtonChecked;
        public bool IsBlocksSettingsSidebarRadioButtonChecked
        {
            get { return _isBlocksSettingsSidebarRadioButtonChecked; }
            set
            {
                if (_isBlocksSettingsSidebarRadioButtonChecked != value)
                {
                    _isBlocksSettingsSidebarRadioButtonChecked = value;
                    OnPropertyChanged(nameof(IsBlocksSettingsSidebarRadioButtonChecked));
                }
            }
        }

        public SettingsViewModel()
        {
            this.UserSettings = SettingsModel.GetUserSettings();

            this.PageTitle = "Settings";
            this.SettingsMenus = new Dictionary<string, object>
            {
                { "User", new SettingsMenu1ViewModel(UserSettings) },
                { "Blocks", new SettingsMenu2ViewModel(UserSettings) }
            };
            
            CurrentSettingsMenu = SettingsMenus["User"];
            UpdateSettingsRadioButtonIsChecked();
            SelectSettingsMenuCommand = new RelayCommand(ChangeSettingsMenu);
        }

        public void ChangeSettingsMenu(object param)
        {
            this.CurrentSettingsMenu = SettingsMenus[param.ToString()];
            UpdateSettingsRadioButtonIsChecked();
        }
        
        public void UpdateSettingsRadioButtonIsChecked()
        {
            if (this.CurrentSettingsMenu is SettingsMenu1ViewModel)
            {
                this.IsUserSettingsSidebarRadioButtonChecked = true;
            }
            else if (this.CurrentSettingsMenu is SettingsMenu2ViewModel)
            {
                this.IsBlocksSettingsSidebarRadioButtonChecked = true;
            }
            else
            {
                this.IsUserSettingsSidebarRadioButtonChecked = true;
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}