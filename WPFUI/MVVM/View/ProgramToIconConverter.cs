using System;
using System.Globalization;
using System.Windows.Data;
using WPFUI.MVVM.ViewModel;

namespace WPFUI.MVVM.View;

public class ProgramToIconConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        string program = (string)values[0];
        return BlocksViewModel.GetExecutableIcon(ProgramUsageModel.GetKnownPrograms()[program].path.ToString());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}