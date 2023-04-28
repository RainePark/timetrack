using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFUI.Core;

namespace WPFUI.MVVM.ViewModel
{
    class BlocksViewModel : IPage
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
        
        public BlocksViewModel()
        {
            this.PageTitle = "Blocks";
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}