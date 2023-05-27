using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFUI.Core;
using WPFUI.MVVM.Model;
using WPFUI.MVVM.ViewModel;

namespace WPFUI.MVVM.View;

public partial class EditBlockView : Window
{
    public ICommand SaveBlock { get; set; }
    
    public EditBlockView(EditBlockViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    
    private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}

