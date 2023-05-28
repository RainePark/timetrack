using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;

namespace WPFUI.MVVM.ViewModel
{
    class DashboardViewModel : IPage
    { 
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
        
        private string _dashboardGreeting;   
        public string DashboardGreeting
        {
            get => this._dashboardGreeting;
            set 
            { 
                this._dashboardGreeting = value; 
                OnPropertyChanged();
            }
        }
        
        private ProgramUsageModel _programUsageModel;
        
        private ObservableCollection<Block> _activeBlocks;   
        public ObservableCollection<Block> ActiveBlocks
        {
            get { return _programUsageModel.ActiveBlocksCollection; }
            set 
            { 
                _programUsageModel.ActiveBlocksCollection = value; 
                OnPropertyChanged();
            }
        }
        
        private string _dashboardText;
        public string DashboardText
        {
            get { return _dashboardGreeting + _programUsageModel.DashboardText; }
            set
            {
                _programUsageModel.DashboardText = value;
                OnPropertyChanged();
            }
        }

        public DashboardViewModel(ProgramUsageModel programUsageModel)
        {
            this.PageTitle = "Dashboard";
            this.DashboardGreeting = CreateDashboardGreeting();
            this._programUsageModel = programUsageModel;
            _programUsageModel.PropertyChanged += ProgramUsageModel_PropertyChanged;
        }

        public string CreateDashboardGreeting()
        {
            string dashboardGreeting;
            Settings userSettings = SettingsModel.GetUserSettings();
            DateTime currentTime = DateTime.Now;string input = userSettings.UserName;

            if (currentTime.Hour >= 5 && currentTime.Hour < 12)
            {
                dashboardGreeting = "Good morning, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
            }
            else if (currentTime.Hour >= 12 && currentTime.Hour < 17)
            {
                dashboardGreeting = "Good afternoon, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
            }
            else if (currentTime.Hour >= 17 && currentTime.Hour < 22)
            {
                dashboardGreeting = "Good evening, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
            }
            else
            {
                dashboardGreeting = "Good night, " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim()) + ".\nYour screen time today is\n";
            }
            return dashboardGreeting;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void ProgramUsageModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProgramUsageModel.DashboardText))
            {
                OnPropertyChanged(nameof(DashboardText));
            }
            if (e.PropertyName == nameof(ProgramUsageModel.ActiveBlocksCollection))
            {
                OnPropertyChanged(nameof(ActiveBlocks));
            }
        }
    }
}