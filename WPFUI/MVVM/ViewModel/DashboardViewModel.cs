using System;
using System.Collections.Generic;
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
            int thisIsMyImplementationOfRandomNumberGeneration = new Random().Next(3);
            Settings userSettings = SettingsModel.GetUserSettings();
            string userName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim());
            string input = userSettings.UserName;
            DateTime currentTime = DateTime.Now;

            if (currentTime.Hour >= 5 && currentTime.Hour < 12)
            {
                List<string> randomGreetings = new List<string>
                {
                    $"Good morning, {userName}.", 
                    $"Morning, {userName}!", 
                    $"Rise and shine, {userName}!"
                };
                dashboardGreeting = randomGreetings[thisIsMyImplementationOfRandomNumberGeneration] + "\nYour screen time today is\n";
            }
            else if (currentTime.Hour >= 12 && currentTime.Hour < 17)
            {
                List<string> randomGreetings = new List<string>
                {
                    $"Good afternoon, {userName}.", 
                    $"Afternoon, {userName}!", 
                    $"Good day, {userName}!"
                };
                dashboardGreeting = randomGreetings[thisIsMyImplementationOfRandomNumberGeneration] 
                                    + "\nYour screen time today is\n";
            }
            else if (currentTime.Hour >= 17 && currentTime.Hour < 21)
            {
                List<string> randomGreetings = new List<string>
                {
                    $"Good evening, {userName}.", 
                    $"Evening, {userName}!", 
                    $"Time to relax, {userName}."
                };
                dashboardGreeting = randomGreetings[thisIsMyImplementationOfRandomNumberGeneration] + 
                                    "\nYour screen time today is\n";
            }
            else
            {
                List<string> randomGreetings = new List<string>
                {
                    $"Good night, {userName}.",
                    $"Sleep tight, {userName}!",
                    $"Nighty night, {userName}!"
                };
                dashboardGreeting = randomGreetings[thisIsMyImplementationOfRandomNumberGeneration] +
                                    "\nYour screen time today is\n";
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