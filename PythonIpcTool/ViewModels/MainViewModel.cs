using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows; // Required for DependencyObject
using PythonIpcTool.Models;
using PythonIpcTool.Services;
using System.IO;
using System.ComponentModel;

namespace PythonIpcTool.ViewModels;

/// <summary>
/// ViewModel for the main application window, handling UI logic and interaction with IPC services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService? _configurationService;
    // Field is now nullable to accommodate design-time instance where it will be null.
    private IPythonProcessCommunicator? _activeCommunicator;

    // Properties for Python interpreter and script paths
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _pythonInterpreterPath = string.Empty;
    partial void OnPythonInterpreterPathChanged(string value) => SaveCurrentSettings();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _pythonScriptPath = string.Empty;

    // Property for user input data
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _inputData = "{\"value\": \"Hello from C#\", \"numbers\": [1, 2, 3]}";
    partial void OnPythonScriptPathChanged(string value) => SaveCurrentSettings();

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
    partial void OnSelectedIpcModeChanged(IpcMode value) => SaveCurrentSettings();

    /// <summary>
    /// Initializes a new instance of the MainViewModel class for the XAML designer.
    /// This constructor is called only when the ViewModel is created in a design tool.
    /// </summary>
    public MainViewModel()
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            // We are in design mode, use a mock/design service.
            _configurationService = new DesignConfigurationService();
            LoadInitialSettings();
            PopulateDesignTimeData();
        }
        else
        {
            // This path should ideally not be taken in a production app with DI.
            // It's a fallback. The constructor with IConfigurationService is preferred.
            // If you use a DI container, this constructor might not even be needed.
        }
    }

    /// <summary>
    /// This parameterless constructor is for the XAML designer ONLY.
    /// It populates the ViewModel with sample data for design-time visualization.
    /// It does NOT perform any runtime logic or service instantiation.
    /// </summary>
    public MainViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

        // Load settings at startup
        LoadInitialSettings();

        Logs.Add("[INFO] Application started. Ready for input.");
    }

    private void PopulateDesignTimeData()
    {
        Logs.Clear();
        Logs.Add("[DESIGN] Application started in design mode.");
        Logs.Add("[DESIGN] This is a log entry visible only in the designer.");
        Logs.Add("[ERROR] This is a design-time error message.");
        OutputResult = "{\"result\": \"This is a sample JSON output shown at design time.\"}";
        IsProcessing = true; // To test the ProgressRing visibility in the designer
    }

    /// <summary>
    /// Loads settings from the configuration service and applies them to the ViewModel properties.
    /// </summary>
    private void LoadInitialSettings()
    {
        var settings = _configurationService.LoadSettings();

        // If loaded script path is empty, provide a sensible default relative path
        string scriptPath = settings.PythonScriptPath;
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "simple_processor.py");
        }

        // Apply loaded settings to the ViewModel properties
        PythonInterpreterPath = settings.PythonInterpreterPath;
        PythonScriptPath = scriptPath;
        SelectedIpcMode = settings.LastUsedIpcMode;
    }

    /// <summary>
    /// Gathers current settings from ViewModel properties and saves them using the configuration service.
    /// </summary>
    private void SaveCurrentSettings()
    {
        var settings = new AppSettings
        {
            PythonInterpreterPath = this.PythonInterpreterPath,
            PythonScriptPath = this.PythonScriptPath,
            LastUsedIpcMode = this.SelectedIpcMode
        };
        _configurationService.SaveSettings(settings);
    }

    // --- Commands ---

    [RelayCommand(CanExecute = nameof(CanExecutePythonScript))]
    private async Task ExecutePythonScriptAsync()
    {
        IsProcessing = true;
        OutputResult = "";
        Logs.Clear();
        Logs.Add($"[INFO] Starting Python script in {SelectedIpcMode} mode...");
        StopPythonProcess();
        IPythonProcessCommunicator? localCommunicator = null;

        // --- MODIFICATION START: Dynamic Communicator Creation ---
        try
        {
            localCommunicator = SelectedIpcMode switch
            {
                IpcMode.StandardIO => new StandardIOProcessCommunicator(),
                IpcMode.LocalSocket => new LocalSocketProcessCommunicator(),
                _ => throw new NotImplementedException($"IPC mode '{SelectedIpcMode}' is not implemented.")
            };

            _activeCommunicator = localCommunicator;
            // Subscribe to its events
            _activeCommunicator.OutputReceived += OnOutputReceived;
            _activeCommunicator.ErrorReceived += OnErrorReceived;
            _activeCommunicator.ProcessExited += OnProcessExited;

            // Now, start the process using the newly created communicator
            await _activeCommunicator.StartProcessAsync(PythonInterpreterPath, PythonScriptPath, SelectedIpcMode);
            Logs.Add($"[INFO] Python process started: {Path.GetFileName(PythonInterpreterPath)}");

            await _activeCommunicator.SendMessageAsync(InputData);
            Logs.Add($"[INFO] Input sent: {InputData}");
        }
        catch (Exception ex)
        {
            OnErrorReceived($"[ERROR] Execution failed: {ex.Message}");
            CleanUpCommunicator(); // Ensure cleanup on failure
            IsProcessing = false;
        }
    }

    // NEW: Add a command to stop the process, which is good for window closing event
    [RelayCommand]
    private void StopPythonProcess()
    {
        Logs.Add("[INFO] Stopping Python process...");
        CleanUpCommunicator();
    }

    // NEW: Helper method for cleanup to avoid code duplication
    private void CleanUpCommunicator()
    {
        if (_activeCommunicator != null)
        {
            // Unsubscribe from events to prevent memory leaks
            _activeCommunicator.OutputReceived -= OnOutputReceived;
            _activeCommunicator.ErrorReceived -= OnErrorReceived;
            _activeCommunicator.ProcessExited -= OnProcessExited;

            _activeCommunicator.StopProcess();
            _activeCommunicator = null;
        }
    }


    private void OnProcessExited(int exitCode)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Logs.Add($"[INFO] Python process exited with code: {exitCode}");
            IsProcessing = false;
            // No need to call StopProcess here anymore, as the process has already exited.
            // The cleanup should happen after we are sure we are done with the communicator instance.
            // Let's call the cleanup helper.
            CleanUpCommunicator();
            ExecutePythonScriptCommand.NotifyCanExecuteChanged();
        });
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

}