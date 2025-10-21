using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PythonIpcTool.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value, but in reverse.
/// True -> Collapsed
/// False -> Visible
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
        {
            // If the value is true, we want to hide the control (Collapsed).
            // If the value is false, we want to show the control (Visible).
            return isVisible ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return false;
    }
}