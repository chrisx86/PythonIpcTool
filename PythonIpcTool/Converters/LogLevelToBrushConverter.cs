using Serilog.Events;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PythonIpcTool.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Error or LogEventLevel.Fatal => Brushes.Red,
                LogEventLevel.Warning => Brushes.Orange,
                LogEventLevel.Information => Brushes.DodgerBlue,
                LogEventLevel.Debug or LogEventLevel.Verbose => Brushes.Gray,
                _ => Brushes.Transparent,
            };
        }
        return Brushes.Transparent;
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
