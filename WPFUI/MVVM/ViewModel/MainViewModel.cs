﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.View;

namespace WPFUI.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public ICommand SelectPageCommand { get; set; }

        private Dictionary<PageName, IPage> Pages { get; }

        private IPage _selectedPage;   
        public IPage SelectedPage
        {
            get => this._selectedPage;
            set 
            { 
                if (_selectedPage != value)
                {
                    this._selectedPage = value; 
                    OnPropertyChanged();
                }
            }
        }
        public MainViewModel()
        {
            SelectPageCommand = new RelayCommand(SelectPage);

            // Create configuration files if they don't exist
            if (!Directory.Exists("user"))
            {
                Directory.CreateDirectory("user");
            }

            try
            {
                JsonConvert.DeserializeObject<Dictionary<string, ProgramDetails>>(File.ReadAllText("user\\programlist.json"));
            }
            catch
            {
                using (StreamWriter writer = new StreamWriter("user\\programlist.json"))
                {
                    writer.WriteLine("{}");
                } 
            }
            
            try
            {
                JsonConvert.DeserializeObject<Dictionary<string,Block>>(File.ReadAllText("user\\blocks.json"));
            }
            catch
            {
                using (StreamWriter writer = new StreamWriter("user\\blocks.json"))
                {
                    writer.WriteLine("{}");
                } 
            }

            try
            {
                JsonConvert.DeserializeObject<Dictionary<string,object>>(File.ReadAllText("user\\blockstatus.json"));
            }
            catch
            {
                using (StreamWriter writer = new StreamWriter("user\\blockstatus.json"))
                {
                    writer.WriteLine("{}");
                } 
            }
            
            try
            {
                SettingsModel.GetUserSettings();
            }
            catch
            {
                WelcomeView welcomeView = new WelcomeView();
                welcomeView.ShowDialog();
            }
            
            this.Pages = new Dictionary<PageName, IPage>
            {
                { PageName.Dashboard, new DashboardViewModel() },
                { PageName.Usage, new UsageViewModel() },
                { PageName.Blocks, new BlocksViewModel() },
                { PageName.Settings, new SettingsViewModel() }
            };
            this.SelectedPage = this.Pages.First().Value;
        }

        public void SelectPage(object param)
        {
            if (param is PageName pageName
                && this.Pages.TryGetValue(pageName, out IPage _selectedPage))
            {
                this.SelectedPage = _selectedPage;
                UpdateRadioButtonIsChecked();
            }
        }

        public void UpdateRadioButtonIsChecked()
        {
            var mainWindow = Application.Current.MainWindow;
            if (this.SelectedPage.PageTitle == "Dashboard")
            {
                var dashboardSidebarRadioButton = (RadioButton)mainWindow.FindName("DashboardSidebarRadioButton");
                dashboardSidebarRadioButton.IsChecked = true;
            }
            else if (this.SelectedPage.PageTitle == "Usage")
            {
                var usageSidebarRadioButton = (RadioButton)mainWindow.FindName("UsageSidebarRadioButton");
                usageSidebarRadioButton.IsChecked = true;
            }
            else if (this.SelectedPage.PageTitle == "Blocks")
            {
                var blocksSidebarRadioButton = (RadioButton)mainWindow.FindName("BlocksSidebarRadioButton");
                blocksSidebarRadioButton.IsChecked = true;
            }
            else if (this.SelectedPage.PageTitle == "Settings")
            {
                var settingsSidebarRadioButton = (RadioButton)mainWindow.FindName("SettingsSidebarRadioButton");
                settingsSidebarRadioButton.IsChecked = true;
            }
            else
            {
                var dashboardSidebarRadioButton = (RadioButton)mainWindow.FindName("DashboardSidebarRadioButton");
                dashboardSidebarRadioButton.IsChecked = true;
            }
        }
    }
}