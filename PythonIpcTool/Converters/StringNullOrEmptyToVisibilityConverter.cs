using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace PythonIpcTool.Converters;

/// <summary>
/// Converts a string to a Visibility value.
/// If the string is null or empty, it returns Collapsed.
/// If the string has content, it returns Visible.
/// Can be inverted by passing "invert" as a parameter.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if the input string is null or empty
        bool isNullOrEmpty = string.IsNullOrEmpty(value as string);

        // Check if the converter parameter is "invert"
        bool invert = parameter is string param && param.Equals("invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
        {
            // Inverted logic: show if null/empty, hide if has content
            return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            // Standard logic: hide if null/empty, show if has content
            return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack is not needed for one-way bindings.
        return Binding.DoNothing;
    }
}
