using System.IO;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand DashboardViewCommand { get; set; }
        public RelayCommand UsageViewCommand { get; set; }
        public DashboardViewModel DashboardVM { get; set; }
        public UsageViewModel UsageVM { get; set; }
        
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }
        
        public MainViewModel()
        {
            DashboardVM = new DashboardViewModel();
            UsageVM = new UsageViewModel();
            
            CurrentView = DashboardVM;

            DashboardViewCommand = new RelayCommand(o =>
            {
                CurrentView = DashboardVM;
            });
            UsageViewCommand = new RelayCommand(o =>
            {
                CurrentView = UsageVM;
            });
        }
    }
}