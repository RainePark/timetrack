using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFUI;

public class PageToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string state = (string)value;
        string parameterString = (string)parameter;
        if (state != null && parameterString != null && state.Equals(parameterString))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isChecked = (bool)value;
        string parameterString = (string)parameter;
        if (isChecked)
        {
            return parameterString;
        }
        else
        {
            return Binding.DoNothing;
        }
    }
}
