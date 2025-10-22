using System.IO;
using System.Windows; // Required for DependencyObject
using System.Text.Json;
using System.ComponentModel;
using System.Collections.ObjectModel;
using ControlzEx.Theming;
using PythonIpcTool.Models;
using PythonIpcTool.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog.Events;
using Serilog;

namespace PythonIpcTool.ViewModels;

/// <summary>
/// ViewModel for the main application window, handling UI logic and interaction with IPC services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private CancellationTokenSource? _cancellationSource;
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
    [NotifyCanExecuteChangedFor(nameof(CancelExecutionCommand))]
    private bool _isProcessing;

    // Property for selecting IPC mode
    [ObservableProperty]
    private IpcMode _selectedIpcMode = IpcMode.StandardIO;
    partial void OnSelectedIpcModeChanged(IpcMode value) => SaveCurrentSettings();

    public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

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
        App.LogEvents.CollectionChanged += OnLogEvent;
        // Load settings at startup
        LoadInitialSettings();

        Log.Information("[INFO] Application started. Ready for input.");
    }

    private void OnLogEvent(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (LogEvent logEvent in e.NewItems)
            {
                // This logic remains the same.
                // We need to dispatch this to the UI thread.
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Add(new LogEntry(logEvent));
                });
            }
        }
    }

    private void PopulateDesignTimeData()
    {
        Logs.Clear();
        Log.Information("[DESIGN] Application started in design mode.");
        Log.Information("[DESIGN] This is a log entry visible only in the designer.");
        Log.Information("[ERROR] This is a design-time error message.");
        OutputResult = "{\"result\": \"This is a sample JSON output shown at design time.\"}";
        IsProcessing = true; // To test the ProgressRing visibility in the designer
    }

    [ObservableProperty]
    private bool _isDarkMode;
    partial void OnIsDarkModeChanged(bool value)
    {
        // Use ThemeManager to change the theme
        ThemeManager.Current.ChangeTheme(Application.Current, value ? "Dark.Blue" : "Light.Blue");
        SaveCurrentSettings();
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
        IsDarkMode = settings.IsDarkMode;
        ThemeManager.Current.ChangeTheme(Application.Current, IsDarkMode ? "Dark.Blue" : "Light.Blue");
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
            LastUsedIpcMode = this.SelectedIpcMode,
            IsDarkMode = this.IsDarkMode
        };
        _configurationService.SaveSettings(settings);
    }

    // --- Commands ---

    [RelayCommand(CanExecute = nameof(CanExecutePythonScript))]
    private async Task ExecutePythonScriptAsync()
    {
        IsProcessing = true;
        //OutputResult = "";
        Logs.Clear();
        Log.Information($"[INFO] Starting Python script in {SelectedIpcMode} mode...");

        _cancellationSource = new CancellationTokenSource();
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
            await _activeCommunicator.StartProcessAsync(PythonInterpreterPath, PythonScriptPath, SelectedIpcMode, _cancellationSource.Token);
            Log.Information($"[INFO] Python process started: {Path.GetFileName(PythonInterpreterPath)}");

            await _activeCommunicator.SendMessageAsync(InputData, _cancellationSource.Token);
            Log.Information($"[INFO] Input sent: {InputData}");
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Execution was canceled by the user.");
            // CRITICAL FIX: The cancellation block MUST clean up its own state.
            // It cannot rely on the OnProcessExited event, which may not fire predictably.
            _cancellationSource?.Cancel();
            IsProcessing = false;
            StopPythonProcess(); // Ensure all resources are released immediately.
        }
        catch (Exception ex)
        {
            Log.Error("Execution was canceled by the user.");
            _cancellationSource?.Cancel();
            StopPythonProcess();
            IsProcessing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelExecution))]
    private void CancelExecution()
    {
        Log.Information("Cancellation requested by user.");
        _cancellationSource?.Cancel();
    }
    private bool CanCancelExecution() => IsProcessing;

    [RelayCommand]
    private void StopPythonProcess()
    {
        Log.Debug("Stopping and cleaning up active communicator.");
        CleanUpCommunicator();
        if (_cancellationSource != null)
        {
            _cancellationSource?.Cancel();
            _cancellationSource.Dispose();
            _cancellationSource = null;
        }
    }

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
            Log.Information($"[INFO] Python process exited with code: {exitCode}");
            IsProcessing = false;
            // No need to call StopProcess here anymore, as the process has already exited.
            // The cleanup should happen after we are sure we are done with the communicator instance.
            // Let's call the cleanup helper.
            StopPythonProcess();
            ExecutePythonScriptCommand.NotifyCanExecuteChanged();
            CancelExecutionCommand.NotifyCanExecuteChanged();
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
        Log.Information("[INFO] Input data cleared.");
    }

    // --- Event Handlers from IPC Communicator ---

    private void OnOutputReceived(string output)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            string formattedOutput = TryFormatJson(output);
            OutputResult += formattedOutput + Environment.NewLine;
            Log.Debug("Python OUT: {Output}", output); // Log raw output at debug level
            IsProcessing = false;
        });
    }

    private string TryFormatJson(string jsonString)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(jsonDoc.RootElement, options);
        }
        catch (JsonException)
        {
            // If it's not valid JSON, return the original string
            return jsonString;
        }
    }

    private void OnErrorReceived(string error)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Log.Information($"[ERROR] {error}");
            IsProcessing = false;
        });
    }

}