namespace WPFUI.MVVM.View;
using System;
using System.Globalization;
using System.Windows.Data;

public class BlockToPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string input = value as string;

        if ((input == "Usage Limit (Combined)") || (input == "Usage Limit (Per App)"))
        {
            return "/Images/stopwatch.ico";
        }
        else if (input == "InputB")
        {
            return "OutputB";
        }
        else
        {
            return "DefaultOutput";
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}