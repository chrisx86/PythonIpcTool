using PythonIpcTool.Models;

namespace PythonIpcTool.Services;

/// <summary>
/// A design-time implementation of IConfigurationService that provides
/// sample data for the XAML designer without accessing the file system.
/// </summary>
public class DesignConfigurationService : IConfigurationService
{
    public AppSettings LoadSettings()
    {
        // Return a new instance with hardcoded design-time values.
        return new AppSettings
        {
            PythonInterpreterPath = @"C:\Path\To\python.exe (Design)",
            PythonScriptPath = @"C:\Path\To\your_script.py (Design)",
            LastUsedIpcMode = IpcMode.LocalSocket
        };
    }

    public void SaveSettings(AppSettings settings)
    {
        // In design mode, we don't need to save anything.
        // This method does nothing.
    }
}