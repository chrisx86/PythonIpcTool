using System.Text.Json.Serialization; // For JSON serialization attributes

namespace PythonIpcTool.Models;

/// <summary>
/// Represents the input data structure sent from C# to the Python script.
/// This model will be serialized into a JSON string.
/// </summary>
public class PythonInputData
{
    /// <summary>
    /// A generic string value for the Python script to process.
    /// Can be adapted based on specific script requirements.
    /// </summary>
    [JsonPropertyName("value")] // Maps to "value" key in JSON
    public string? Value { get; set; }

    /// <summary>
    /// An optional list of numbers for numerical processing in Python.
    /// </summary>
    [JsonPropertyName("numbers")] // Maps to "numbers" key in JSON
    public List<double>? Numbers { get; set; }

    /// <summary>
    /// An optional custom payload for more complex data.
    /// This could be any JSON-serializable object.
    /// </summary>
    [JsonPropertyName("customPayload")] // Maps to "customPayload" key in JSON
    public object? CustomPayload { get; set; }

    // You can add more properties here as per your Python script's input requirements.
    // For example:
    // [JsonPropertyName("command")]
    // public string? Command { get; set; }
}