using System.Globalization;
using System.Windows.Data;

namespace PythonIpcTool.Converters;

/// <summary>
/// Converts an Enum value to a boolean, used for RadioButtons.
/// </summary>
public class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString()?.Equals(parameter.ToString(), StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter != null)
        {
            return Enum.Parse(targetType, parameter.ToString() ?? string.Empty);
        }
        return Binding.DoNothing;
    }
}