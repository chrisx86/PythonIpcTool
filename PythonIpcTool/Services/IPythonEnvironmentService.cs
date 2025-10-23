using System.Diagnostics;
using System.IO;
using System.Text;

public interface IPythonEnvironmentService
{
    // Event to stream command output to the UI
    event Action<string> OutputReceived;

    /// <summary>
    /// Creates a virtual environment in the specified directory.
    /// </summary>
    /// <param name="globalPythonPath">Path to the global python.exe used to create the venv.</param>
    /// <param name="projectDirectory">The directory where the 'venv' folder will be created.</param>
    /// <returns>The path to the python.exe inside the newly created venv, or null if failed.</returns>
    Task<string?> CreateVenvAsync(string globalPythonPath, string projectDirectory);

    /// <summary>
    /// Installs dependencies from a requirements.txt file using the venv's pip.
    /// </summary>
    /// <param name="venvPythonPath">Path to the python.exe inside the virtual environment.</param>
    /// <param name="requirementsPath">Path to the requirements.txt file.</param>
    /// <returns>True if installation was successful, otherwise false.</returns>
    Task<bool> InstallDependenciesAsync(string venvPythonPath, string requirementsPath);
}

public class PythonEnvironmentService : IPythonEnvironmentService
{
    public event Action<string>? OutputReceived;

    public async Task<string?> CreateVenvAsync(string globalPythonPath, string projectDirectory)
    {
        var venvPath = Path.Combine(projectDirectory, "venv");
        if (Directory.Exists(venvPath))
        {
            OutputReceived?.Invoke("Virtual environment 'venv' already exists in this directory.");
            return Path.Combine(venvPath, "Scripts", "python.exe");
        }

        return await RunProcessAsync(globalPythonPath, $"-m venv \"{venvPath}\"", "Creating virtual environment...")
            ? Path.Combine(venvPath, "Scripts", "python.exe")
            : null;
    }

    public async Task<bool> InstallDependenciesAsync(string venvPythonPath, string requirementsPath)
    {
        return await RunProcessAsync(venvPythonPath, $"-m pip install -r \"{requirementsPath}\"", "Installing dependencies...");
    }

    private async Task<bool> RunProcessAsync(string fileName, string arguments, string startMessage)
    {
        OutputReceived?.Invoke(startMessage);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            }
        };

        process.OutputDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) OutputReceived?.Invoke($"[ERROR] {e.Data}"); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        bool success = process.ExitCode == 0;
        OutputReceived?.Invoke(success ? "Operation completed successfully." : "Operation failed.");
        return success;
    }
}