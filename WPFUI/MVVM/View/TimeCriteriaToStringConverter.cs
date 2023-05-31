using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WPFUI.MVVM.View;

public class TimeCriteriaToStringConverter : IValueConverter
{
    // Converter to show the days that a list of days the block is active on the dashboard
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if the value is a list of strings
        if (value is IEnumerable<string> list)
        {
            // Check if the list contains all seven days of the week
            if (list.Count() == 7)
            {
                return "Everyday";
            }
            else
            {
                // Create a string of the days the block is active
                // This is trimmed to 2 letters for each day as it would not fit in the box on the dashboard otherwise
                var modifiedList = list.Select(s => s.Substring(0, s.Length - 1));
                return string.Join(", ", modifiedList);
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}