using System;
using System.Globalization;
using System.Windows.Data;
using WPFUI.MVVM.ViewModel;

namespace WPFUI.MVVM.View;

// Converts a program name to an ImageSource object (type BitmapImage) that corresponds to the program
public class ProgramToIconConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Uses a function to the executable icon based on the input program name
        string program = (string)values[0];
        return BlocksViewModel.GetExecutableIcon(ProgramUsageModel.GetKnownPrograms()[program].path.ToString());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}