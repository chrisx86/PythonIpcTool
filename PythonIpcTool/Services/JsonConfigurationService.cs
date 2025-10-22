using System.IO;
using System.Text.Json;
using PythonIpcTool.Models;
using Serilog;

namespace PythonIpcTool.Services;

/// <summary>
/// Implements IConfigurationService to save and load settings to a JSON file
/// in the user's local application data folder.
/// </summary>
public class JsonConfigurationService : IConfigurationService
{
    // Define the path where the settings file will be stored.
    // Using AppData is the standard practice for user-specific configuration.
    private static readonly string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static readonly string AppFolder = Path.Combine(AppDataFolder, "PythonIpcTool");
    private static readonly string SettingsFilePath = Path.Combine(AppFolder, "settings.json");

    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                // Attempt to deserialize. If file is corrupt or empty, return default.
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            // Log the error if loading fails (e.g., file corruption, permission issues)
            // For now, we'll just fall back to default settings.
            Log.Error($"Error loading settings: {ex.Message}");
        }

        // If file doesn't exist or loading failed, return a new instance with default values.
        return new AppSettings();
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            // Ensure the application directory exists.
            Directory.CreateDirectory(AppFolder);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            // Log the error if saving fails.
            // The application should still function, but settings won't be saved.
            Log.Error($"Error saving settings: {ex.Message}");
        }
    }
}