using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public ICommand SelectPageCommand => new RelayCommand(SelectPage);

        private Dictionary<PageName, IPage> Pages { get; }

        private IPage _selectedPage;   
        public IPage SelectedPage
        {
            get => this._selectedPage;
            set 
            { 
                this._selectedPage = value; 
                OnPropertyChanged();
            }
        }
        public MainViewModel()
        {
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
            }
        }
    }
}