using Serilog;
using Serilog.Events;
using System.Collections.ObjectModel; // REQUIRED for ObservableCollection
using System.Windows;
using Serilog.Sinks.Observable;       // REQUIRED for the .ObservableCollection() extension method
using Serilog.Sinks.ObservableCollection;
namespace PythonIpcTool;
public partial class App : Application
{
    public static ObservableCollection<LogEvent> LogEvents { get; } = new ObservableCollection<LogEvent>();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        Log.Information("Application Starting Up...");

        // Setup global exception handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "An unobserved task exception occurred.");
        // Optionally, show a message to the user
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "An unhandled UI exception occurred.");
        // Prevent the application from crashing
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application Shutting Down...");
        Log.CloseAndFlush(); // Ensure all logs are written before exit
        base.OnExit(e);
    }
}