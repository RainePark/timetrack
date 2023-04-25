namespace WPFUI.MVVM.View;
using System;
using System.Windows.Data;

public class BooleanToStateTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Boolean)
        {
            return (bool)value ? "Enabled" : "Disabled";
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is string)
        {
            return ((string)value) == "Enabled";
        }

        return value;
    }
}