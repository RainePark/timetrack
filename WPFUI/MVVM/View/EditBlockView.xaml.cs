using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        // Set the datacontext to the viewmodel of the EditBlockViewModel that is spawned when the window is opened. This allows for the block details to be accessed from binding in XAML
        DataContext = viewModel;
    }
    
    // Set the selected items of the listbox to match the saved data in the block time criteria
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
    
    // Update the block time criteria when the ListBox is updated by the user
    private void TimeCriteriaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Get the details of the element in the UI that called the function
        ListBox listBox = sender as ListBox;
        List<string> selectedItems = new List<string>();
        // Loop through the selected items in the ListBox and add them to the selected days of the block
        foreach (object selectedItem in listBox.SelectedItems)
        {
            selectedItems.Add(selectedItem.ToString());
        }
        // Update the block in the viewmodel
        ((EditBlockViewModel)DataContext).SelectedTimeCriteria = selectedItems;
    }
    
    // Validate the input in the block time criteria to make sure only numbers have been put in
    private void BlockTimeTextBox_Unfocused(object sender, RoutedEventArgs e)
    {
        TextBox textBox = sender as TextBox;
        if (!Regex.IsMatch(textBox.Text.Trim(), @"^(\d{0,2})?$")){
            MessageBox.Show("Please enter a valid number");
            textBox.Text = "";
        }
    }
    
    // Confirms that the user would like to exit the edit block page without saving
    private void CloseWindowButton_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBoxResult result = MessageBox.Show("Are you sure you want to close without saving?", "Confirmation", MessageBoxButton.YesNo);
        if (result == MessageBoxResult.Yes)
        {
            Close();
        }
    }
    
    // Allow the edit block window to be dragged around
    private void TitleBar_DragWindow(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }
}