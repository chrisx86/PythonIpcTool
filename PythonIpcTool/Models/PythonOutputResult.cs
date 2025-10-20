using System.Text.Json.Serialization; // For JSON serialization attributes

namespace PythonIpcTool.Models;

/// <summary>
/// Represents the output data structure received from the Python script.
/// This model will be deserialized from a JSON string.
/// </summary>
public class PythonOutputResult
{
    /// <summary>
    /// The main processing result from the Python script.
    /// </summary>
    [JsonPropertyName("result")] // Maps to "result" key in JSON
    public string? Result { get; set; }

    /// <summary>
    /// The status of the operation (e.g., "success", "failure", "processing").
    /// </summary>
    [JsonPropertyName("status")] // Maps to "status" key in JSON
    public string? Status { get; set; }

    /// <summary>
    /// An optional error message if the Python script encountered an issue.
    /// </summary>
    [JsonPropertyName("error")] // Maps to "error" key in JSON
    public string? Error { get; set; }

    /// <summary>
    /// An optional custom data object for more complex return data.
    /// </summary>
    [JsonPropertyName("customData")] // Maps to "customData" key in JSON
    public object? CustomData { get; set; }

    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    [JsonIgnore] // This property will not be serialized/deserialized to/from JSON
    public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(Error);

    // You can add more properties here as per your Python script's output requirements.
}