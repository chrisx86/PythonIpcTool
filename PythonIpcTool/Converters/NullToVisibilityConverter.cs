using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace PythonIpcTool.Converters;

/// <summary>
/// Converts an object to a System.Windows.Visibility value.
/// If the object is null, it returns Visibility.Collapsed.
/// If the object is not null, it returns Visibility.Visible.
/// Can be inverted by passing "invert" as the converter parameter.
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a value.
    /// </summary>
    /// <param name="value">The value produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use. Can be "invert".</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool invert = parameter is string param && param.Equals("invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
        {
            // Inverted logic: show if null, hide if not null
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            // Standard logic: hide if null, show if not null
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    //  Converts a value back. Not typically used for visibility.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ConvertBack is not implemented as it's not needed for this one-way conversion scenario.
        return Binding.DoNothing;
    }
}
