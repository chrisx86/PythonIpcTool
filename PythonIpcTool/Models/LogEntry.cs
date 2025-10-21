using Serilog.Events;

public class LogEntry
{
    public DateTimeOffset Timestamp { get; }
    public LogEventLevel Level { get; }
    public string Message { get; }

    public LogEntry(LogEvent logEvent)
    {
        Timestamp = logEvent.Timestamp;
        Level = logEvent.Level;
        Message = logEvent.RenderMessage();
    }
}