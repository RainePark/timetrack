using System.Collections.ObjectModel;
using System.ComponentModel;
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
        
        private string _dashboardText;
        public string DashboardText
        {
            get { return _programUsageModel.DashboardText; }
            set
            {
                _programUsageModel.DashboardText = value;
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