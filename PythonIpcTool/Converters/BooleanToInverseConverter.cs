using System.Globalization;
using System.Windows.Data;

namespace PythonIpcTool.Converters;

/// <summary>
/// A simple boolean to inverse boolean converter.
/// </summary>
public class BooleanToInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return !boolValue;
        return false;
    }
}
