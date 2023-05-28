using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using WPFUI.MVVM.Model;

namespace WPFUI.MVVM.View;

public class CriteriaToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Block block)
        {
            if ((block.Type == "Usage Limit (Combined)") || (block.Type == "Usage Limit (Per App)"))
            {
                return $"{block.Conditions[1].Criteria[0]} hour, {block.Conditions[1].Criteria[1]} minute limit today";
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}