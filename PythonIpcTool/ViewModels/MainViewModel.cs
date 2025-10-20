using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel; // Required for DesignerProperties
using System.Windows; // Required for DependencyObject
using PythonIpcTool.Models;
using PythonIpcTool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PythonIpcTool.ViewModels;

/// <summary>
/// ViewModel for the main application window, handling UI logic and interaction with IPC services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // Field is now nullable to accommodate design-time instance where it will be null.
    private readonly IPythonProcessCommunicator? _ipcCommunicator;

    // Properties for Python interpreter and script paths
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _pythonInterpreterPath = "C:\\Python310\\python.exe";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _pythonScriptPath = "E:\\Project\\VisualStudio\\PythonIpcTool\\PythonIpcTool\\PythonScripts\\simple_processor.py";

    // Property for user input data
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _inputData = "{\"value\": \"Hello from C#\", \"numbers\": [1, 2, 3]}";

    // Property for displaying Python script's output
    [ObservableProperty]
    private string _outputResult = "";

    // Property for displaying application status and logs
    public ObservableCollection<string> Logs { get; } = new ObservableCollection<string>();

    // Property to indicate if a process is currently running (for UI busy state)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private bool _isProcessing;

    // Property for selecting IPC mode
    [ObservableProperty]
    private IpcMode _selectedIpcMode = IpcMode.StandardIO;

    /// <summary>
    /// This constructor is for runtime use and is typically called with dependency injection.
    /// </summary>
    public MainViewModel(IPythonProcessCommunicator ipcCommunicator)
    {
        _ipcCommunicator = ipcCommunicator ?? throw new ArgumentNullException(nameof(ipcCommunicator));

        // Subscribe to IPC communicator events
        _ipcCommunicator.OutputReceived += OnOutputReceived;
        _ipcCommunicator.ErrorReceived += OnErrorReceived;
        _ipcCommunicator.ProcessExited += OnProcessExited;

        Logs.Add("[INFO] Application started. Ready for input.");
    }

    /// <summary>
    /// This parameterless constructor is for the XAML designer ONLY.
    /// It populates the ViewModel with sample data for design-time visualization.
    /// It does NOT perform any runtime logic or service instantiation.
    /// </summary>
    public MainViewModel()
    {
        // We can keep the design-time check as a safeguard.
        if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            // Allow inheritance for runtime scenarios if needed, but for now, we assume
            // the parameterless constructor is only for the designer.
            // In a real DI scenario, this constructor might not even exist or be protected.
            // For now, let's keep the exception to prevent misuse.
            throw new InvalidOperationException("This constructor is intended for design-time use only.");
        }

        _ipcCommunicator = null;

        PythonInterpreterPath = @"C:\Path\To\Your\python.exe (Design Time)";
        PythonScriptPath = @"C:\Path\To\Your\script.py (Design Time)";
        InputData = "{\"message\": \"Sample JSON data for designer\"}";
        OutputResult = "This is a sample output result shown only in the designer.";
        Logs.Add("[DESIGN] ViewModel loaded in design mode.");
        Logs.Add("[DESIGN] This is a sample log entry.");
        IsProcessing = true;
    }

    // --- Commands ---

    [RelayCommand(CanExecute = nameof(CanExecutePythonScript))]
    private async Task ExecutePythonScriptAsync()
    {
        // Add a null check for robustness. This prevents crashes if used in an invalid state.
        if (_ipcCommunicator == null)
        {
            OnErrorReceived("[ERROR] IPC Communicator is not initialized.");
            return;
        }

        IsProcessing = true;
        OutputResult = "";
        Logs.Clear();
        Logs.Add($"[INFO] Starting Python script in {SelectedIpcMode} mode...");

        try
        {
            await _ipcCommunicator.StartProcessAsync(PythonInterpreterPath, PythonScriptPath, SelectedIpcMode);
            Logs.Add($"[INFO] Python process started: {PythonInterpreterPath} {PythonScriptPath}");
            await _ipcCommunicator.SendMessageAsync(InputData);
            Logs.Add($"[INFO] Input sent: {InputData}");
        }
        catch (Exception ex)
        {
            OnErrorReceived($"[ERROR] Execution failed: {ex.Message}");
            _ipcCommunicator.StopProcess();
            IsProcessing = false;
        }
    }

    private bool CanExecutePythonScript()
    {
        return !string.IsNullOrWhiteSpace(PythonInterpreterPath) &&
               !string.IsNullOrWhiteSpace(PythonScriptPath) &&
               !string.IsNullOrWhiteSpace(InputData) &&
               !IsProcessing;
    }

    /// <summary>
    /// Command to browse for the Python interpreter executable.
    /// This method is wrapped into 'BrowsePythonInterpreterCommand' by the source generator.
    /// </summary>
    [RelayCommand]
    private void BrowsePythonInterpreter()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Python Executable|python.exe;pythonw.exe|All Files (*.*)|*.*",
            Title = "Select Python Interpreter"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            PythonInterpreterPath = openFileDialog.FileName;
        }
    }

    /// <summary>
    /// Command to browse for the Python script file.
    /// This method is wrapped into 'BrowsePythonScriptCommand' by the source generator.
    /// </summary>
    [RelayCommand]
    private void BrowsePythonScript()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Python Scripts (*.py)|*.py|All Files (*.*)|*.*",
            Title = "Select Python Script"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            PythonScriptPath = openFileDialog.FileName;
        }
    }

    /// <summary>
    /// Command to clear the input data text.
    /// This method is wrapped into 'ClearInputCommand' by the source generator.
    /// </summary>
    [RelayCommand]
    private void ClearInput()
    {
        InputData = "";
        Logs.Add("[INFO] Input data cleared.");
    }

    [RelayCommand]
    private void StopPythonProcess()
    {
        Logs.Add("[INFO] Stop command issued. Terminating Python process if running.");
        // Use null-conditional operator for safety, as communicator is null in design mode.
        _ipcCommunicator?.StopProcess();
        IsProcessing = false;
    }

    // --- Event Handlers from IPC Communicator ---

    private void OnOutputReceived(string output)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            OutputResult += output + Environment.NewLine;
            Logs.Add($"[PYTHON OUT] {output}");
        });
    }

    private void OnErrorReceived(string error)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Add($"[ERROR] {error}");
        });
    }

    private void OnProcessExited(int exitCode)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Add($"[INFO] Python process exited with code: {exitCode}");
            IsProcessing = false;
            _ipcCommunicator?.StopProcess();
            (ExecutePythonScriptCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        });
    }
}