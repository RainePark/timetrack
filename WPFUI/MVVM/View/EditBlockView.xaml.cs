using System.Collections.Generic;
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
    
    private void TimeCriteriaListBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Get a reference to the ListBox
        ListBox listBox = sender as ListBox;
        
        // Create a list of items to select
        List<string> selectedItems = ((EditBlockViewModel)DataContext).UpdatedBlockData.Conditions[0].Value.TimeCriteria;

        // Clear the current selection
        listBox.SelectedItems.Clear();

        // Loop through each item in the ListBox
        foreach (string item in listBox.Items)
        {
            // Check if the item is in the SelectedTimeCriteria list
            if (selectedItems.Contains(item))
            {
                // Set the item to selected
                listBox.SelectedItems.Add(item);
            }
        }
    }
    
    private void TimeCriteriaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ListBox listBox = sender as ListBox;
        List<string> selectedItems = new List<string>();
        foreach (object selectedItem in listBox.SelectedItems)
        {
            selectedItems.Add(selectedItem.ToString());
        }
        ((EditBlockViewModel)DataContext).SelectedTimeCriteria = selectedItems;
    }
    
    private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("Are you sure you want to close without saving?", "Confirmation", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            Close();
        }
    }
    
    private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}