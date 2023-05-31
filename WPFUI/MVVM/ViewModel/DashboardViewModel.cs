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
        // Define properties with OnPropertyChanged() calls to update the UI when they change
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
        
        // Allow the view to access the model's ActiveBlocksCollection and DashboardText properties
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
            // Generate the greeting message
            this.DashboardGreeting = CreateDashboardGreeting();
            // Use a program usage model that is shared with the main view model so that only 1 instance of the model is used
            // This is important as the program usage model is responsible for tracking program usage which would be inaccurate if there were multiple instances
            this._programUsageModel = programUsageModel;
            _programUsageModel.PropertyChanged += ProgramUsageModel_PropertyChanged;
        }
        
        // Create the greeting message based on the user's name and the time of day
        public string CreateDashboardGreeting()
        {
            string dashboardGreeting;
            // Implement RNG to select a random greeting WOW!!!!!111!1!1!1!1!!!!! this is so RAD!!! (Rapid Application Development)
            int thisIsMyImplementationOfRandomNumberGeneration = new Random().Next(3);
            // Get the user name from the settings file
            Settings userSettings = SettingsModel.GetUserSettings();
            string userName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userSettings.UserName.ToLower().Trim());
            // Get the current time
            DateTime currentTime = DateTime.Now;
            // Generate a greeting based on time of day, selecting a random greeting from a list of 3 based on the RNG implementation
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
        
        // Implement PropertyChanged to ensure that the UI is updated when the Model and ViewModel changes
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