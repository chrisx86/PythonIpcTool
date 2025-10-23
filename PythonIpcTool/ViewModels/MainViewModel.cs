using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows; // Required for DependencyObject
using System.Collections; // Ensure this is present for AsyncRelayCommand
using System.ComponentModel;
using System.Collections.ObjectModel;
using ControlzEx.Theming;
using PythonIpcTool.Models;
using PythonIpcTool.Services;
using PythonIpcTool.Exceptions;
using PythonIpcTool.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using Serilog.Events;
using MahApps.Metro.Controls.Dialogs;
using System.Text.RegularExpressions; // For more advanced cleanup if needed


namespace PythonIpcTool.ViewModels;

/// <summary>
/// ViewModel for the main application window, handling UI logic and interaction with IPC services.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IDisposable _logSubscription;
    private readonly IConfigurationService? _configurationService;
    private readonly IDialogCoordinator _dialogCoordinator;
    private readonly IPythonEnvironmentService _envService;
    // Field is now nullable to accommodate design-time instance where it will be null.
    private IPythonProcessCommunicator? _activeCommunicator;
    private ScriptProfile? _oldSelectedProfile;
    private CancellationTokenSource? _cancellationSource;
    private bool _isInitialized = false;
    private int _executionCount = 0;

    [ObservableProperty]
    private bool _isEnvironmentBusy;

    public ObservableCollection<ScriptProfile> ScriptProfiles { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))] // 當切換 Profile 時重新檢查按鈕狀態
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedProfileCommand))]
    private ScriptProfile? _selectedScriptProfile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveOrUpdateProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallDependenciesCommand))] // And this one
    private string _pythonInterpreterPath = "python.exe";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    [NotifyCanExecuteChangedFor(nameof(CreateVenvCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveOrUpdateProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallDependenciesCommand))]
    private string _pythonScriptPath = "";

    // Property for user input data
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecutePythonScriptCommand))]
    private string _inputData = "{\"value\": \"Hello from C#\", \"numbers\": [1, 2, 3]}";

    [ObservableProperty]
    private IpcMode _selectedIpcMode = IpcMode.StandardIO;

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

    public ObservableCollection<LogEntry> LogEntries { get; } = new ();

    [ObservableProperty]
    private string _virtualEnvStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallDependenciesCommand))]// This is important for enabling/disabling the install button
    private bool _isVirtualEnvDetected = false;

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
    public MainViewModel(IConfigurationService configurationService, IDialogCoordinator dialogCoordinator, IPythonEnvironmentService envService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _dialogCoordinator = dialogCoordinator ?? throw new ArgumentNullException(nameof(dialogCoordinator));
        ScriptProfiles = new ObservableCollection<ScriptProfile>();
        _envService = envService;

        _envService.OutputReceived += (log) => Log.Information(log); // Pipe output to our logger

        App.LogEvents.CollectionChanged += OnLogEvent;
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

        Log.Information("Application started. Ready for input.");
    }

    // This partial method is automatically called by the source generator
    // whenever the PythonInterpreterPath property changes.
    partial void OnPythonInterpreterPathChanged(string value)
    {
        // When the path changes, re-run the virtual environment check.
        CheckForVirtualEnvironment(value);

        // Also, if a profile is selected, we should update its data.
        if (SelectedScriptProfile != null)
        {
            SelectedScriptProfile.PythonInterpreterPath = value;
        }
    }

    // For consistency, do the same for the script path.
    partial void OnPythonScriptPathChanged(string value)
    {
        if (SelectedScriptProfile != null)
        {
            SelectedScriptProfile.PythonScriptPath = value;
        }
        // Although not directly related to venv detection, this keeps the profile updated.
        // It also ensures the CanExecute for InstallDependenciesCommand is re-evaluated.
        InstallDependenciesCommand.NotifyCanExecuteChanged();
    }

    // NEW: Command to create a virtual environment
    [RelayCommand(CanExecute = nameof(CanCreateVenv))]
    private async Task CreateVenvAsync()
    {
        if (string.IsNullOrWhiteSpace(PythonScriptPath) || !File.Exists(PythonScriptPath))
        {
            await _dialogCoordinator.ShowMessageAsync(this, "Error", "Please select a valid Python script first to determine the project directory.");
            return;
        }

        IsEnvironmentBusy = true;
        try
        {
            string projectDirectory = Path.GetDirectoryName(PythonScriptPath)!;
            string? newPythonPath = await _envService.CreateVenvAsync("python", projectDirectory);
            if (newPythonPath != null && File.Exists(newPythonPath))
            {
                // Automatically update the path to the new venv interpreter
                PythonInterpreterPath = newPythonPath;
            }
        }
        finally
        {
            IsEnvironmentBusy = false;
        }
    }

    private bool CanCreateVenv() => !IsEnvironmentBusy && !IsProcessing;

    // NEW: Command to install dependencies
    [RelayCommand(CanExecute = nameof(CanInstallDependencies))]
    private async Task InstallDependenciesAsync()
    {
        // We need to get the directory of the script to find requirements.txt
        string? scriptDir = Path.GetDirectoryName(PythonScriptPath);
        if (scriptDir == null)
        {
            Log.Error("Could not determine the script directory.");
            return;
        }
        var requirementsPath = Path.Combine(scriptDir, "requirements.txt");

        Log.Information("Starting dependency installation from {RequirementsFile}...", requirementsPath);
        IsProcessing = true; // Set busy state

        try
        {
            // We create a temporary StandardIOProcessCommunicator just for this task
            var installerCommunicator = new StandardIOProcessCommunicator();

            // We don't need a script, we pass arguments to pip directly
            var arguments = $"-m pip install -r \"{requirementsPath}\"";
            Log.Information("Command to install dependencies: {command}", arguments);
            // We need a way to capture the output of this process
            var installLogs = new StringBuilder();
            installerCommunicator.OutputReceived += (log) => installLogs.AppendLine(log);
            installerCommunicator.ErrorReceived += (log) => installLogs.AppendLine($"[ERROR] {log}");

            var processExitedTcs = new TaskCompletionSource<int>();
            installerCommunicator.ProcessExited += (exitCode) => processExitedTcs.SetResult(exitCode);

            Log.Information("Wait for the process to complete...");

            // Start the process without a script, but with arguments
            await installerCommunicator.StartProcessAsync(PythonInterpreterPath, arguments, IpcMode.StandardIO, CancellationToken.None);

            int exitCode = await processExitedTcs.Task; // Wait for the process to complete

            if (exitCode == 0)
            {
                Log.Information("Dependencies installed successfully.");
                await _dialogCoordinator.ShowMessageAsync(this, "Success", "Dependencies were installed successfully into the virtual environment.");
            }
            else
            {
                Log.Error("Failed to install dependencies. Exit code: {ExitCode}. See details below:\n{InstallLogs}", exitCode, installLogs.ToString());
                await _dialogCoordinator.ShowMessageAsync(this, "Error", $"Failed to install dependencies. Please check the logs for details.\n\nOutput:\n{installLogs.ToString()}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An exception occurred while trying to install dependencies.");
            await _dialogCoordinator.ShowMessageAsync(this, "Error", $"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanInstallDependencies()
    {
        // Condition 1: Must not be busy
        if (IsProcessing) return false;

        // Condition 2: Must have a virtual environment detected
        if (!_isVirtualEnvDetected) return false;

        // Condition 3: A requirements.txt file must exist next to the selected script
        string? scriptDir = Path.GetDirectoryName(PythonScriptPath);
        if (string.IsNullOrWhiteSpace(scriptDir)) return false;

        if (!File.Exists(Path.Combine(scriptDir, "requirements.txt"))) return false;

        return true;
    }

    // --- NEW: Implement IDisposable to clean up the subscription ---
    /// <summary>
    /// Cleans up resources, particularly the log event subscription.
    /// </summary>
    public void Dispose()
    {
        // This method should be called when the ViewModel is no longer needed.
        // For the main window, this can be called in the Window_Closed event.
        _logSubscription.Dispose();
        StopPythonProcess(); // Ensure any running process is also stopped.
        GC.SuppressFinalize(this);
    }

    // --- NEW: Commands for Profile Management ---
    // This partial method is now the key to loading a profile into the workspace.
    partial void OnSelectedScriptProfileChanged(ScriptProfile? value)
    {
        if (value != null)
        {
            // Load the selected profile's data into the current workspace.
            PythonInterpreterPath = value.PythonInterpreterPath;
            PythonScriptPath = value.PythonScriptPath;
            SelectedIpcMode = value.SelectedIpcMode;
            InputData = value.InputData;
            Log.Information("Loaded profile: {ProfileName}", value.Name);
        }
        // Save the fact that this profile was the last one selected.
        SaveAppSettings();
    }

    private void SaveAppSettings()
    {
        // This is a simplified save method that doesn't save the whole profile list on every change.
        // It's assumed you have a more intelligent save trigger now (e.g., on exit, or when profiles change).
        if (!_isInitialized) return;

        var settings = new AppSettings
        {
            ScriptProfiles = this.ScriptProfiles.ToList(),
            LastSelectedProfileId = this.SelectedScriptProfile?.Id,
            IsDarkMode = this.IsDarkMode
        };
        _configurationService.SaveSettings(settings);
    }

    [RelayCommand(CanExecute = nameof(CanSaveOrUpdateProfile))]
    private async Task SaveOrUpdateProfileAsync()
    {
        // Case 1: A profile IS selected. We UPDATE it.
        if (SelectedScriptProfile != null)
        {
            // Update the existing profile object with the values from the "Current Configuration"
            SelectedScriptProfile.PythonInterpreterPath = this.PythonInterpreterPath;
            SelectedScriptProfile.PythonScriptPath = this.PythonScriptPath;
            SelectedScriptProfile.SelectedIpcMode = this.SelectedIpcMode;
            SelectedScriptProfile.InputData = this.InputData;

            // Note: The Name property is not updated here, as renaming should typically be a separate action.
            // If you want to allow renaming, you would bind the Name TextBox to SelectedScriptProfile.Name directly.

            Log.Information("Profile '{ProfileName}' has been updated.", SelectedScriptProfile.Name);

            // Manually trigger a save of all settings.
            SaveAppSettings();
        }
        // Case 2: NO profile is selected. We CREATE a new one.
        else
        {
            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Save",
                NegativeButtonText = "Cancel",
                DefaultText = $"Profile {ScriptProfiles.Count + 1}"
            };

            string? profileName = await _dialogCoordinator.ShowInputAsync(this, "Save New Profile", "Enter a name for the new profile:", settings);

            if (string.IsNullOrWhiteSpace(profileName))
            {
                Log.Information("Saving new profile was canceled.");
                return;
            }

            var newProfile = new ScriptProfile
            {
                Name = profileName,
                PythonInterpreterPath = this.PythonInterpreterPath,
                PythonScriptPath = this.PythonScriptPath,
                InputData = this.InputData,
                SelectedIpcMode = this.SelectedIpcMode
            };

            ScriptProfiles.Add(newProfile);
            SelectedScriptProfile = newProfile; // This automatically selects the new profile and triggers a save.
            Log.Information("New profile '{ProfileName}' saved successfully.", profileName);
        }
    }

    // --- NEW: CanExecute logic for the save button ---
    private bool CanSaveOrUpdateProfile()
    {
        // The save button should be enabled as long as the current configuration paths are not empty.
        // We don't need to check File.Exists here, as the user might be saving a profile for another machine.
        return !string.IsNullOrWhiteSpace(PythonInterpreterPath) &&
               !string.IsNullOrWhiteSpace(PythonScriptPath);
    }

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

    private bool CanRemoveProfile() => SelectedScriptProfile != null;

    // This event handler is triggered when a property *inside* the selected profile changes
    private void SelectedProfile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Save settings whenever the name, path, or IPC mode of the selected profile is edited.
        SaveCurrentSettings();
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
                ScriptProfiles.Add(profile);
            }
            // Load the last selected profile into the workspace, if it exists.
            var lastProfile = ScriptProfiles.FirstOrDefault(p => p.Id == settings.LastSelectedProfileId);
            if (lastProfile != null)
            {
                // Set the ComboBox selection WITHOUT triggering the full load logic initially
                // to avoid double loading.
                _selectedScriptProfile = lastProfile;
                OnPropertyChanged(nameof(SelectedScriptProfile)); // Manually notify UI

                // Then, explicitly load its data.
                PythonInterpreterPath = lastProfile.PythonInterpreterPath;
                PythonScriptPath = lastProfile.PythonScriptPath;
                SelectedIpcMode = lastProfile.SelectedIpcMode;
                InputData = lastProfile.InputData;
            }
            else
            {
                // No last profile, just use defaults.
                PythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "simple_processor.py");
            }
        }
        else
        {
            // First time running, use defaults. No profile is created automatically.
            PythonScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonScripts", "simple_processor.py");
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
        IsProcessing = true;
        //OutputResult = "";
        Logs.Clear();
        _executionCount++;
        string ordinalExecution = _executionCount.ToOrdinal(); // Use your extension method!
        if (_executionCount > 1)
        {
            // Add some space and a separator line before the next output
            OutputResult += $"{Environment.NewLine}{Environment.NewLine}--- {ordinalExecution} Execution ---{Environment.NewLine}";
        }
        else
        {
            OutputResult = $"--- {_executionCount.ToOrdinal()} Execution ---{Environment.NewLine}";
        }
        Log.Information("[INFO] Starting {Ordinal} Python script execution in {Mode} mode...", ordinalExecution, SelectedIpcMode);
        _cancellationSource = new CancellationTokenSource();
        IPythonProcessCommunicator? localCommunicator = null;
        try
        {
            (bool isValid, string processedInput) = PreprocessJsonInput(InputData);
            // If preprocessing determines the input is fundamentally invalid (e.g., completely unparseable),
            // we log it and stop, but after the UI state has been correctly set.
            if (!isValid)
            {
                Log.Error("Input data could not be parsed as valid JSON or a Python dictionary. Please check the format.");
                // We don't need a MessageBox here as the log provides the feedback.
                return; // Stop execution
            }
            Log.Information("Sending processed input to Python: {Input}", processedInput);

            localCommunicator = this.SelectedIpcMode switch
            {
                IpcMode.StandardIO => new StandardIOProcessCommunicator(),
                IpcMode.LocalSocket => new LocalSocketProcessCommunicator(),
                // The default case now throws a very specific exception.
                _ => throw new InvalidOperationException($"IPC mode '{this.SelectedIpcMode}' is not supported.")
            };
            _activeCommunicator = localCommunicator;
            // Subscribe to its events
            _activeCommunicator.OutputReceived += OnOutputReceived;
            _activeCommunicator.ErrorReceived += OnErrorReceived;
            _activeCommunicator.ProcessExited += OnProcessExited;

            await _activeCommunicator.StartProcessAsync(
                this.PythonInterpreterPath,
                this.PythonScriptPath,
                this.SelectedIpcMode,
                _cancellationSource.Token);

            // Now, start the process using the newly created communicator
            Log.Information($"[INFO] Python process started: {Path.GetFileName(this.PythonInterpreterPath)}");
            // IMPORTANT: Check for cancellation *before* sending the message.
            // This prevents the "SendMessageAsync was canceled" warning in normal exit scenarios.
            if (_cancellationSource.IsCancellationRequested)
            {
                IsProcessing = false;
                throw new OperationCanceledException();
            }
            else
            {
                await _activeCommunicator.SendMessageAsync(processedInput, _cancellationSource.Token);
                Log.Information($"[INFO] Input sent to Python script: {Path.GetFileName(this.PythonScriptPath)} {processedInput}");
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
        }
        finally
        {
            // THIS IS THE GUARANTEED CLEANUP POINT for a single execution task.
            if (IsProcessing)
            {
                IsProcessing = false;
            }
            StopPythonProcess(); // This will clean up the communicator for this task.
        }
    }

    private bool CanCancelExecution() => IsProcessing;

    [RelayCommand(CanExecute = nameof(CanCancelExecution))]
    private void CancelExecution()
    {
        Log.Information("Cancellation requested by user.");
        _cancellationSource?.Cancel();
    }

    [RelayCommand]
    public void StopPythonProcess()
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
        return !string.IsNullOrWhiteSpace(PythonInterpreterPath) &&
               File.Exists(PythonInterpreterPath) &&
               !string.IsNullOrWhiteSpace(PythonScriptPath) &&
               File.Exists(PythonScriptPath) &&
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

        if (openFileDialog.ShowDialog() == true)
        {
            // --- CORRECTED LOGIC ---
            // 1. Always update the main "Current Configuration" property.
            PythonScriptPath = openFileDialog.FileName;

            // 2. (UX Improvement) Deselect the current profile.
            SelectedScriptProfile = null;
        }
    }

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
            // --- CORRECTED LOGIC ---
            // 1. Always update the main "Current Configuration" property.
            PythonInterpreterPath = openFileDialog.FileName;
            CheckForVirtualEnvironment(PythonInterpreterPath);

            // 2. (UX Improvement) Deselect the current profile to indicate a custom configuration.
            // This makes it clear that the current settings no longer match any saved profile.
            SelectedScriptProfile = null;
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
            Log.Debug("Python OUT for {Ordinal} run: {Output}", _executionCount.ToOrdinal(), output);
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

    [RelayCommand]
    private void CopySelectedLogs(object? selectedItems)
    {
        if (selectedItems is IList items && items.Count > 0)
        {
            var logEntries = items.OfType<LogEntry>();
            var sb = new StringBuilder();
            foreach (var log in logEntries)
            {
                sb.AppendLine($"[{log.Timestamp:HH:mm:ss.fff}] [{log.Level}] {log.Message}");
            }

            try
            {
                // SetText can sometimes fail if the clipboard is in use by another process.
                Clipboard.SetText(sb.ToString());
                Log.Information("Copied {Count} log entries to clipboard.", logEntries.Count());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy logs to clipboard.");
            }
        }
    }

    /// <summary>
    /// Validates and preprocesses the raw input string into a compact, single-line format.
    /// </summary>
    /// <param name="rawInput">The raw input from the TextBox.</param>
    /// <returns>A tuple containing a boolean indicating validity and the processed string.</returns>
    private (bool IsValid, string ProcessedString) PreprocessJsonInput(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            // An empty input is valid and can be treated as an empty JSON object.
            return (true, "{}");
        }

        // First, try to parse as strict JSON (handles multi-line correctly).
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(rawInput))
            {
                // If successful, serialize to a compact single-line string. This is the ideal path.
                return (true, JsonSerializer.Serialize(doc.RootElement));
            }
        }
        catch (JsonException)
        {
            // If strict JSON parsing fails, it might be a Python dict with single quotes.
            // We will perform a basic cleanup and let Python's ast.literal_eval try to handle it.

            // We consider this path "valid" for the purpose of attempting execution.
            // Python will be the final judge.

            // Collapse all whitespace and newlines into single spaces.
            string singleLine = Regex.Replace(rawInput, @"\s+", " ").Trim();

            return (true, singleLine);
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputResult = "";
        _executionCount = 0; // Reset the counter as well
        Log.Information("Output has been cleared.");
    }
}