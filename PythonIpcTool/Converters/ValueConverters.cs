using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PythonIpcTool.Converters
{
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

    /// <summary>
    /// Converts a log entry string to a theme-aware color based on its content.
    /// </summary>
    public class LogEntryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string logEntry)
            {
                if (logEntry.StartsWith("[ERROR]", StringComparison.OrdinalIgnoreCase))
                {
                    return Brushes.Red;
                }
                if (logEntry.StartsWith("[WARN]", StringComparison.OrdinalIgnoreCase))
                {
                    // Using a more theme-friendly color than pure Yellow
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFD700") ?? Brushes.Orange);
                }
                if (logEntry.StartsWith("[PYTHON OUT]", StringComparison.OrdinalIgnoreCase))
                {
                    return Brushes.GreenYellow;
                }
            }
            // Use a system resource that adapts to the current theme (Light/Dark)
            // This ensures text is always readable.
            return new DynamicResourceExtension(SystemColors.ControlTextBrushKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

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
}