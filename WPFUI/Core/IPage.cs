using System.ComponentModel;

namespace WPFUI.Core;

// Implement standard interface for the ViewModels of the submenus to ensure they all implement INotifyPropertyChanged
// This mainly is used to remind me to implement INotifyChanged when I make a new viewmodel. 
// PageTitle matches the value in PageName.cs and ensures that if the name of a page is changed it is automatically reflected in the code. 
interface IPage : INotifyPropertyChanged
{
    string PageTitle { get; set; }
}