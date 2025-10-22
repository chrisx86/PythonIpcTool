using Serilog;
using Serilog.Events;
using System.Collections.ObjectModel; // REQUIRED for ObservableCollection
using System.Windows;
using Serilog.Sinks.Observable;       // REQUIRED for the .ObservableCollection() extension method
using Serilog.Sinks.ObservableCollection;
using System.Windows.Threading;
namespace PythonIpcTool;
public partial class App : Application
{
    //public static ObservableCollection<LogEvent> LogEvents { get; } = new ObservableCollection<LogEvent>();
    //public static ObservableSink LogEventsSink { get; } = new ObservableSink();
    //public static IObservable<LogEvent> LogEvents { get; }
    public static ObservableCollection<LogEvent> LogEvents { get; } = new ObservableCollection<LogEvent>();
    static App()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/app_log_.txt",
                          rollingInterval: RollingInterval.Day,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

            // Using the correct property name 'MaxStoredEvents' as defined in your project's version of the library.
            .WriteTo.Sink(
                new ObservableCollectionSink(
                    // 1. The collection to write to.
                    LogEvents,
                    // 2. The UI thread dispatcher.
                    action => Current.Dispatcher.Invoke(action),
                    // 3. The required options object with the correct property name.
                    new ObservableCollectionSinkOptions
                    {
                        // Correct property to limit the number of items in the collection.
                        MaxStoredEvents = 1000
                    }
                ),
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Information("Application Starting Up...");
        SetupGlobalExceptionHandling();
    }

    private void SetupGlobalExceptionHandling()
    {
        // Catch exceptions from the UI thread
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Catch exceptions from background tasks
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Log the unhandled exception
        Log.Fatal(e.Exception, "An unhandled exception occurred on the UI thread.");

        // Show a friendly message to the user
        MessageBox.Show(
            $"An unexpected error occurred. The application might need to close.\n\nPlease check the logs for more details.\n\nError: {e.Exception.Message}",
            "Unhandled Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        // Prevent the application from crashing
        e.Handled = true;

        // Optionally, you might want to gracefully shutdown
        // Current.Shutdown();
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Log the unobserved task exception
        Log.Fatal(e.Exception, "An unobserved task exception occurred.");

        // Set the exception as observed to prevent the process from terminating
        e.SetObserved();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application Shutting Down...");
        Log.CloseAndFlush(); // Ensure all logs are written before exit
        base.OnExit(e);
    }
}