using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace PythonIpcTool.Converters;

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
