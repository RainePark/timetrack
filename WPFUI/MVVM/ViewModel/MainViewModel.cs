using System.Collections.Generic;
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
        // Set up commands for the view to bind to
        public ICommand SelectPageCommand { get; set; }
        public ICommand EditBlockCommand { get; set; }
        
        // Set up the page switching variables
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

        // Set up the program usage model that tracks program usage
        private ProgramUsageModel _programUsageModel;
        public ProgramUsageModel ProgramUsageModel
        {
            get => this._programUsageModel;
            set 
            { 
                if (_programUsageModel != value)
                {
                    this._programUsageModel = value; 
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
            SelectPageCommand = new RelayCommand(SelectPage);
            EditBlockCommand = new RelayCommand(EditBlock_Clicked);
            
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
            
            // Check if the user has settings set up
            try
            {
                SettingsModel.GetUserSettings();
            }
            // Run the welcome screen if they have not
            catch
            {
                WelcomeView welcomeView = new WelcomeView();
                welcomeView.ShowDialog();
            }

            // Start the program usage model
            _programUsageModel = new ProgramUsageModel();

            // Set up the pages
            this.Pages = new Dictionary<PageName, IPage>
            {
                { PageName.Dashboard, new DashboardViewModel(ProgramUsageModel) },
                { PageName.Usage, new UsageViewModel() },
                { PageName.Blocks, new BlocksViewModel() },
                { PageName.Settings, new SettingsViewModel() }
            };
            // Set the page on startup to the dashboard
            this.SelectedPage = this.Pages.First().Value;
        }

        // Command to switch pages
        public void SelectPage(object param)
        {
            // Gets the page from the dictionary
            if (param is PageName pageName
                && this.Pages.TryGetValue(pageName, out IPage _selectedPage))
            {
                // Create new viewmodels for the pages that need to be refreshed when switching to them
                if (_selectedPage.PageTitle == "Dashboard")
                {
                    this.Pages[PageName.Dashboard] = new DashboardViewModel(ProgramUsageModel);
                    _selectedPage = this.Pages[PageName.Dashboard];
                }
                if (_selectedPage.PageTitle == "Usage")
                {
                    this.Pages[PageName.Usage] = new UsageViewModel();
                    _selectedPage = this.Pages[PageName.Usage];
                }
                // Set the new page
                this.SelectedPage = _selectedPage;
                // Update the selected sidebar menu
                UpdateRadioButtonIsChecked();
            }
        }
        
        // Command to edit a block shown in the active blocks list
        // Note: this is currently not used as the edit button was removed
        private void EditBlock_Clicked(object sender)
        {
            string blockName = ((Button)sender).Tag.ToString();
            Block block = BlocksModel.GetAllBlocks()[blockName];
            if (block != null)
            {
                EditBlockView editBlock = new EditBlockView(new EditBlockViewModel(block, this));
                editBlock.ShowDialog();
            }
        }

        // Update the selected sidebar menu
        public void UpdateRadioButtonIsChecked()
        {
            // Sets the sidebar menu to the selected page
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