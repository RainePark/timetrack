using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFUI.Core;

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