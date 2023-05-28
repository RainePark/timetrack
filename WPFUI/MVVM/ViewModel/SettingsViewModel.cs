using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;

namespace WPFUI.MVVM.ViewModel
{
    class SettingsViewModel : IPage
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
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

        public string lastValidName;

        public SettingsViewModel()
        {
            this.PageTitle = "Settings";
            this.UserSettings = SettingsModel.GetUserSettings();
            this.UserSettings.PropertyChanged += UserSettings_PropertyChanged;
            this.lastValidName = UserSettings.UserName.ToString();
        }
        
        public ObservableCollection<string> BlockTypeComboBoxItems { get; } = new ObservableCollection<string>
        {
            "Exit Program",
            "Minimise Window"
        };

        private void UserSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Call the desired function when the UserName property is updated
            if (e.PropertyName == "UserName")
            {
                if (string.IsNullOrEmpty(this.UserSettings.UserName))
                {
                    this.UserSettings.UserName = lastValidName;
                    MessageBox.Show("Name cannot be empty");
                    return;
                }
                Regex regex1 = new Regex(@"^[a-zA-Z0-9]+$");
                if (!regex1.IsMatch(this.UserSettings.UserName))
                {
                    this.UserSettings.UserName = lastValidName;
                    MessageBox.Show("Name can only contain alphanumeric characters and cannot contain spaces");
                    return;
                }
                Regex regex2 = new Regex(@"[a-zA-Z]");
                if (!regex2.IsMatch(this.UserSettings.UserName))
                {
                    this.UserSettings.UserName = lastValidName;
                    MessageBox.Show("Name must contain at least one alphabetical character");
                    return;
                }
                SettingsModel.WriteSettings(this.UserSettings);
                this.lastValidName = UserSettings.UserName;
            }
            if (e.PropertyName == "SystemApps")
            {
                SettingsModel.WriteSettings(this.UserSettings);
            }
            if (e.PropertyName == "BlockType")
            {
                SettingsModel.WriteSettings(this.UserSettings);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}