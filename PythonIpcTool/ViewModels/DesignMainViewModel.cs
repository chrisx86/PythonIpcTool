namespace PythonIpcTool.ViewModels;

/// <summary>
/// A design-time ViewModel that provides sample data for the XAML designer.
/// This class inherits from MainViewModel but is only used for design purposes.
/// Its constructor is guaranteed to be safe for the designer to execute.
/// </summary>
public class DesignMainViewModel : MainViewModel
{
    /// <summary>
    /// Initializes a new instance of the DesignMainViewModel class.
    /// This constructor is safe to call from the XAML designer.
    /// </summary>
    public DesignMainViewModel() : base() // Call the base parameterless constructor
    {
        // The base parameterless constructor already handles the design-time check and data population.
        // You can override or add more specific design-time data here if needed.
        // For example:
        PythonInterpreterPath = @"C:\Python\python.exe (Design Instance)";
        PythonScriptPath = @"C:\Scripts\my_script.py (Design Instance)";
        Logs.Add("[DESIGN] ViewModel loaded via d:DesignInstance.");
    }
}