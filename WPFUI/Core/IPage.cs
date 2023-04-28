using System.ComponentModel;

namespace WPFUI.Core;

interface IPage : INotifyPropertyChanged
{
    string PageTitle { get; set; }
}