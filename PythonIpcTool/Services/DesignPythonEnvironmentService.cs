namespace PythonIpcTool.Services;

/// <summary>
/// A design-time safe implementation of IPythonEnvironmentService that does nothing.
/// This is used to satisfy the MainViewModel's constructor in the XAML designer.
/// </summary>
public class DesignPythonEnvironmentService : IPythonEnvironmentService
{
    // The event will never be raised in design time.
    public event Action<string>? OutputReceived;

    /// <summary>
    /// Simulates creating a venv but does nothing and returns a dummy path.
    /// </summary>
    public Task<string?> CreateVenvAsync(string globalPythonPath, string projectDirectory)
    {
        _ = globalPythonPath;
        _ = projectDirectory;
        _ = OutputReceived;

        // Return a completed task with a null result, indicating no path was created.
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Simulates installing dependencies but does nothing and returns success.
    /// </summary>
    public Task<bool> InstallDependenciesAsync(string venvPythonPath, string requirementsPath)
    {
        _ = venvPythonPath;
        _ = requirementsPath;

        // Return a completed task with a 'true' result.
        return Task.FromResult(true);
    }
}