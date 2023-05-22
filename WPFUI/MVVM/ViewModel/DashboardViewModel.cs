using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFUI.Core;
using WPFUI.MVVM.Model;

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
        
        private ProgramUsageModel _programUsageModel;
        
        private int _usageHours;
        public int UsageHours
        {
            get { return _programUsageModel.UsageHours; }
            set
            {
                _programUsageModel.UsageHours = value;
                OnPropertyChanged();
            }
        }
        
        private int _usageMinutes;
        public int UsageMinutes
        {
            get { return _programUsageModel.UsageMinutes; }
            set
            {
                _programUsageModel.UsageMinutes = value;
                OnPropertyChanged();
            }
        }

        public DashboardViewModel()
        {
            this.PageTitle = "Dashboard";
            _programUsageModel = new ProgramUsageModel();
            _programUsageModel.PropertyChanged += ProgramUsageModel_PropertyChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void ProgramUsageModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProgramUsageModel.UsageHours))
            {
                OnPropertyChanged(nameof(UsageHours));
            }
            if (e.PropertyName == nameof(ProgramUsageModel.UsageMinutes))
            {
                OnPropertyChanged(nameof(UsageMinutes));
            }
            if (e.PropertyName == nameof(ProgramUsageModel.ActiveBlocksCollection))
            {
                OnPropertyChanged(nameof(ActiveBlocks));
            }
        }
    }
}