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
using PythonIpcTool.Exceptions;

namespace PythonIpcTool.ViewModels;

/// <summary>
/// ViewModel for the main application window, handling UI logic and interaction with IPC services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IConfigurationService? _configurationService;
    // Field is now nullable to accommodate design-time instance where it will be null.
    private IPythonProcessCommunicator? _activeCommunicator;
    private ScriptProfile? _oldSelectedProfile;
    private bool _isInitialized = false;
    private CancellationTokenSource? _cancellationSource;

    public ObservableCollection<ScriptProfile> ScriptProfiles { get; } = new();
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))] // 當切換 Profile 時重新檢查按鈕狀態
    private ScriptProfile? _selectedScriptProfile;



    private bool CanRemoveProfile() => SelectedScriptProfile != null;

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
    [NotifyCanExecuteChangedFor(nameof(CancelExecutionCommand))]
    private bool _isProcessing;

    // Property for selecting IPC mode
    //[ObservableProperty]
    //private IpcMode _selectedIpcMode = IpcMode.StandardIO;
    //partial void OnSelectedIpcModeChanged(IpcMode value) => UpdateSelectedProfilePaths();

    public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

    // --- NEW: Property for Virtual Environment Status ---
    [ObservableProperty]
    private string _virtualEnvStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isVirtualEnvDetected = false;

    // --- NEW: Commands for Profile Management ---
    [RelayCommand]
    private void AddNewProfile()
    {
        var newProfile = new ScriptProfile();
        ScriptProfiles.Add(newProfile);
        SelectedScriptProfile = newProfile;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveProfile))]
    private void RemoveSelectedProfile()
    {
        if (SelectedScriptProfile != null)
        {
            int selectedIndex = ScriptProfiles.IndexOf(SelectedScriptProfile);
            ScriptProfiles.Remove(SelectedScriptProfile);

            // Select the next item in the list, or the last one if the removed item was the last.
            if (ScriptProfiles.Any())
            {
                SelectedScriptProfile = ScriptProfiles[Math.Min(selectedIndex, ScriptProfiles.Count - 1)];
            }
            else
            {
                SelectedScriptProfile = null;
            }
        }
    }

    // This event handler is triggered when a property *inside* the selected profile changes
    private void SelectedProfile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Save settings whenever the name, path, or IPC mode of the selected profile is edited.
        SaveCurrentSettings();
    }

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

        ScriptProfiles = new ObservableCollection<ScriptProfile>();
        LoadInitialSettings();
        _isInitialized = true;

        // --- NEW: Subscribe to property changes within the selected profile ---
        // This allows us to save automatically when the user edits the current profile's details.
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SelectedScriptProfile))
            {
                SaveCurrentSettings(); // Save when the profile selection changes

                if (_oldSelectedProfile != null)
                {
                    _oldSelectedProfile.PropertyChanged -= SelectedProfile_PropertyChanged;
                }

                if (SelectedScriptProfile != null)
                {
                    SelectedScriptProfile.PropertyChanged += SelectedProfile_PropertyChanged;
                }
                _oldSelectedProfile = SelectedScriptProfile;
            }
        };

        Logs.Add("[INFO] Application started. Ready for input.");
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
        if (settings.ScriptProfiles.Any())
        {
            foreach (var profile in settings.ScriptProfiles)
            {
                // `ScriptProfiles` is now a valid, initialized collection here.
                ScriptProfiles.Add(profile);

            }
            // `SelectedScriptProfile` is assigned here, using the now-populated `ScriptProfiles`.
            SelectedScriptProfile = ScriptProfiles.FirstOrDefault(p => p.Id == settings.LastSelectedProfileId) ?? ScriptProfiles.First();
        }
        else
        {
            AddNewProfile();
        }

        // Dark mode is a global setting, so it's loaded here
        IsDarkMode = settings.IsDarkMode;
        ThemeManager.Current.ChangeTheme(Application.Current, IsDarkMode ? "Dark.Blue" : "Light.Blue");
    }

    /// <summary>
    /// Gathers current settings from ViewModel properties and saves them using the configuration service.
    /// </summary>
    private void SaveCurrentSettings()
    {
        if (!_isInitialized) return; // Exit if the ViewModel is not fully initialized yet.

        var settings = new AppSettings
        {
            ScriptProfiles = this.ScriptProfiles.ToList(),
            LastSelectedProfileId = this.SelectedScriptProfile?.Id,
            IsDarkMode = this.IsDarkMode
        };
        _configurationService.SaveSettings(settings);
        Log.Debug("Settings saved.");
    }

    // --- Commands ---

    [RelayCommand(CanExecute = nameof(CanExecutePythonScript))]
    private async Task ExecutePythonScriptAsync()
    {
        if (SelectedScriptProfile == null) return;
        try
        {
            JsonDocument.Parse(InputData);
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Invalid JSON format in input data.");
            // Optionally show a message box to the user here
            MessageBox.Show($"The input data is not a valid JSON.\n\nDetails: {ex.Message}", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return; // Stop execution
        }
        IsProcessing = true;
        //OutputResult = "";
        Logs.Clear();
        Log.Information($"[INFO] Starting Python script in {SelectedScriptProfile.SelectedIpcMode} mode...");

        _cancellationSource = new CancellationTokenSource();

        // --- MODIFICATION START: Dynamic Communicator Creation ---
        try
        {
            IPythonProcessCommunicator communicator = SelectedScriptProfile.SelectedIpcMode switch
            {
                IpcMode.StandardIO => new StandardIOProcessCommunicator(),
                IpcMode.LocalSocket => new LocalSocketProcessCommunicator(),
                // The default case now throws a very specific exception.
                _ => throw new InvalidOperationException($"IPC mode '{SelectedScriptProfile.SelectedIpcMode}' is not supported.")
            };
            _activeCommunicator = communicator;
            // Subscribe to its events
            _activeCommunicator.OutputReceived += OnOutputReceived;
            _activeCommunicator.ErrorReceived += OnErrorReceived;
            _activeCommunicator.ProcessExited += OnProcessExited;

            await _activeCommunicator.StartProcessAsync(
                SelectedScriptProfile.PythonInterpreterPath,
                SelectedScriptProfile.PythonScriptPath,
                SelectedScriptProfile.SelectedIpcMode, // 使用 Profile 中的模式
                _cancellationSource.Token);

            // Now, start the process using the newly created communicator
            Log.Information($"[INFO] Python process started: {Path.GetFileName(SelectedScriptProfile.PythonInterpreterPath)}");
            // IMPORTANT: Check for cancellation *before* sending the message.
            // This prevents the "SendMessageAsync was canceled" warning in normal exit scenarios.
            if (_cancellationSource.IsCancellationRequested)
            {
                IsProcessing = false;
                throw new OperationCanceledException();
            }
            else
            {
                await _activeCommunicator.SendMessageAsync(InputData, _cancellationSource.Token);
                Log.Information($"[INFO] Input sent to Python script: {InputData}");
            }
        }
        // --- MODIFICATION: More specific exception handling ---
        catch (OperationCanceledException)
        {
            Log.Warning("Execution was canceled by the user.");
            IsProcessing = false;
        }
        catch (PythonProcessException ex)
        {
            // Catch our specific exception for detailed logging
            Log.Error(ex, "A problem occurred with the Python process.");
            IsProcessing = false;
        }
        catch (Exception ex)
        {
            // Catch any other unexpected exceptions
            Log.Error(ex, "An unexpected error occurred during Python script execution.");
            IsProcessing = false;
            StopPythonProcess(); // Clean up if setup fails
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
        Log.Information("Python process exited with code: {ExitCode}", exitCode);
        // --- REVISED LOGIC ---
        // Perform cleanup operations on a background thread to avoid blocking the UI.
        // Task.Run is a simple way to ensure this.
        Task.Run(() =>
        {
            // This is the cleanup logic that was causing the deadlock/slowness.
            // It's now safely on a background thread.
            StopPythonProcess();
        });

        // Dispatch ONLY the necessary UI updates to the UI thread.
        // These operations are guaranteed to be fast.
        App.Current.Dispatcher.Invoke(() =>
        {
            if (IsProcessing) // Check if we are still in a processing state
            {
                IsProcessing = false;
                ExecutePythonScriptCommand.NotifyCanExecuteChanged();
                CancelExecutionCommand.NotifyCanExecuteChanged();
            }
        });
    }

    private bool CanExecutePythonScript()
    {
        if (SelectedScriptProfile == null) return false;

        return !string.IsNullOrWhiteSpace(SelectedScriptProfile.PythonInterpreterPath) &&
               File.Exists(SelectedScriptProfile.PythonInterpreterPath) &&
               !string.IsNullOrWhiteSpace(SelectedScriptProfile.PythonScriptPath) &&
               File.Exists(SelectedScriptProfile.PythonScriptPath) &&
               !IsProcessing;
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

        if (openFileDialog.ShowDialog() == true && SelectedScriptProfile != null)
        {
            SelectedScriptProfile.PythonScriptPath = openFileDialog.FileName;
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


    [RelayCommand]
    private void BrowsePythonInterpreter()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Python Executable|python.exe;pythonw.exe|All Files (*.*)|*.*",
            Title = "Select Python Interpreter"
        };

        if (openFileDialog.ShowDialog() == true && SelectedScriptProfile != null)
        {
            SelectedScriptProfile.PythonInterpreterPath = openFileDialog.FileName;
        }
    }

    // --- NEW: Helper method for detection ---
    /// <summary>
    /// Checks if the provided Python interpreter path is inside a standard virtual environment.
    /// Updates UI properties based on the result.
    /// </summary>
    private void CheckForVirtualEnvironment(string interpreterPath)
    {
        if (string.IsNullOrWhiteSpace(interpreterPath) || !File.Exists(interpreterPath))
        {
            VirtualEnvStatusMessage = string.Empty;
            IsVirtualEnvDetected = false;
            return;
        }

        try
        {
            var interpreterDir = Path.GetDirectoryName(interpreterPath);
            if (interpreterDir == null) return;

            // Standard venv check (looks for pyvenv.cfg in the parent directory)
            var venvRootDir = Directory.GetParent(interpreterDir);
            if (venvRootDir != null && File.Exists(Path.Combine(venvRootDir.FullName, "pyvenv.cfg")))
            {
                VirtualEnvStatusMessage = $"Virtual Environment Detected: {venvRootDir.Name}";
                IsVirtualEnvDetected = true;
                return;
            }

            // Conda env check (looks for 'conda-meta' directory in the parent of the parent)
            if (venvRootDir?.Parent != null && Directory.Exists(Path.Combine(venvRootDir.Parent.FullName, "conda-meta")))
            {
                VirtualEnvStatusMessage = $"Conda Environment Detected: {venvRootDir.Parent.Name}";
                IsVirtualEnvDetected = true;
                return;
            }

            // If no specific environment is found, clear the message.
            VirtualEnvStatusMessage = "No virtual environment detected (using global Python?).";
            IsVirtualEnvDetected = false;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to check for virtual environment.");
            VirtualEnvStatusMessage = "Could not determine environment status.";
            IsVirtualEnvDetected = false;
        }
    }

}