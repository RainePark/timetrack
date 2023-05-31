namespace WPFUI.MVVM.View;
using System;
using System.Windows.Data;

public class BooleanToStateTextConverter : IValueConverter
{
    // Implement Converter for Block to show Enabled or Disabled
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is Boolean)
        {
            // Return Enabled or Disabled depending on the state of the Block
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