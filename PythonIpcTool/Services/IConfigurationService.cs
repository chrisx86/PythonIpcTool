using PythonIpcTool.Models;

namespace PythonIpcTool.Services;

/// <summary>
/// Defines the contract for a service that manages loading and saving application settings.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Loads the application settings from a persistent storage.
    /// If no settings are found, it should return a new instance with default values.
    /// </summary>
    /// <returns>An instance of AppSettings.</returns>
    AppSettings LoadSettings();

    /// <summary>
    /// Saves the provided application settings to a persistent storage.
    /// </summary>
    /// <param name="settings">The AppSettings instance to save.</param>
    void SaveSettings(AppSettings settings);
}