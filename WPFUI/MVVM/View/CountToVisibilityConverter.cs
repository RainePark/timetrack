namespace WPFUI.MVVM.View;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class CountToVisibilityConverter : IValueConverter
{
    // Implement Converter to set an element to be visible if there is no data in the list
    // This is used on the dashboard to show a message if there are no active blocks
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Return Visibility.Collapsed if there is data in the list
        if (value is int count)
        {
            return count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}