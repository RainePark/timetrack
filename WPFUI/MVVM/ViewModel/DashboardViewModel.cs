using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFUI.Core;

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
        
        private ProgramUsageModel _programUsageModel;

        public DashboardViewModel()
        {
            this.PageTitle = "Dashboard";

            _programUsageModel = new ProgramUsageModel();
            _programUsageModel.PropertyChanged += ProgramUsageModel_PropertyChanged;
        }

        private string _currentProgram;
        public string CurrentProgram
        {
            get { return _programUsageModel.CurrentProgram; }
            set
            {
                _programUsageModel.CurrentProgram = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        private void ProgramUsageModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProgramUsageModel.CurrentProgram))
            {
                OnPropertyChanged(nameof(CurrentProgram));
            }
        }
    }
}