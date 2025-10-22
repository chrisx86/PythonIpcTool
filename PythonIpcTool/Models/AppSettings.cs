using System.IO;
using System.Text.Json.Serialization;

namespace PythonIpcTool.Models;

/// <summary>
/// Represents the application's user-configurable settings that can be persisted.
/// </summary>
public class AppSettings
{
    // The list of all saved script profiles
    [JsonPropertyName("scriptProfiles")]
    public List<ScriptProfile> ScriptProfiles { get; set; } = new();

    // The ID of the last selected profile
    [JsonPropertyName("lastSelectedProfileId")]
    public Guid? LastSelectedProfileId { get; set; }

    [JsonPropertyName("isDarkMode")]
    public bool IsDarkMode { get; set; } = false; // Default to light mode
    /// <summary>
    /// The file path to the Python interpreter executable.
    /// </summary>
    [JsonPropertyName("pythonInterpreterPath")]
    public string PythonInterpreterPath { get; set; } = "C:\\Python310\\python.exe"; // Default value

    /// <summary>
    /// The file path to the Python script to be executed.
    /// </summary>
    [JsonPropertyName("pythonScriptPath")]
    public string PythonScriptPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "simple_processor.py");

    /// <summary>
    /// The last selected Inter-Process Communication mode.
    /// </summary>
    [JsonPropertyName("lastUsedIpcMode")]
    public IpcMode LastUsedIpcMode { get; set; } = IpcMode.StandardIO; // Default to StandardIO
}