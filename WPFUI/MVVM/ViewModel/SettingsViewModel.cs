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
        
        public ICommand UserNameTextBoxUnfocused { get; set; } 
        public string lastValidName;

        public SettingsViewModel()
        {
            this.PageTitle = "Settings";
            this.UserSettings = SettingsModel.GetUserSettings();
            this.UserSettings.PropertyChanged += UserSettings_PropertyChanged;
            // Set the last valid name to the current name in case the user enters an invalid name
            this.lastValidName = UserSettings.UserName.ToString();
            UserNameTextBoxUnfocused = new RelayCommand(UserNameTextBox_Unfocused);
        }
        
        // Define valid options for the block behaviour
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
                // Check to make sure that the name is not empty
                if (string.IsNullOrEmpty(this.UserSettings.UserName))
                {
                    // Do not update the name if it is empty
                    // Does not show a message box as this is handled by the UserNameTextBox_Unfocused function
                    // This is because the user may wish to empty the box before entering a new name
                    return;
                }
                // Check to make sure that the name does not contain any invalid characters
                Regex regex1 = new Regex(@"^[a-zA-Z0-9]+$");
                if (!regex1.IsMatch(this.UserSettings.UserName))
                {
                    this.UserSettings.UserName = lastValidName;
                    MessageBox.Show("Name can only contain alphanumeric characters and cannot contain spaces");
                    return;
                }
                // Check to make sure that the name contains at least one alphabetical character
                Regex regex2 = new Regex(@"[a-zA-Z]");
                if (!regex2.IsMatch(this.UserSettings.UserName))
                {
                    this.UserSettings.UserName = lastValidName;
                    MessageBox.Show("Name must contain at least one alphabetical character");
                    return;
                }
                // Write the settings to the file if the name is valid
                SettingsModel.WriteSettings(this.UserSettings);
            }
            // Update the settings file when the SystemApps or BlockType property is updated
            if (e.PropertyName == "SystemApps")
            {
                SettingsModel.WriteSettings(this.UserSettings);
            }
            if (e.PropertyName == "BlockType")
            {
                SettingsModel.WriteSettings(this.UserSettings);
            }
        }
        
        // Check if the name is empty when the user unfocuses the text box
        private void UserNameTextBox_Unfocused(object parameter)
        {
            // Check to make sure that the name is not empty
            if (string.IsNullOrEmpty(this.UserSettings.UserName))
            {
                this.UserSettings.UserName = lastValidName;
                MessageBox.Show("Name cannot be empty");
                return;
            }
            // Write the settings to the file if the name is valid
            SettingsModel.WriteSettings(this.UserSettings);
            // Update the last valid name
            this.lastValidName = UserSettings.UserName;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}