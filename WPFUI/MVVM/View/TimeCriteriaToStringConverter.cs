using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WPFUI.MVVM.View;

public class TimeCriteriaToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<string> list)
        {
            if (list.Count() == 7)
            {
                return "Everyday";
            }
            else
            {
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