/// <summary>
/// Defines the inter-process communication (IPC) modes available.
/// </summary>
public enum IpcMode
{
    /// <summary>
    /// Communication via Standard Input/Output streams.
    /// </summary>
    StandardIO,
    /// <summary>
    /// Communication via local TCP Sockets.
    /// </summary>
    LocalSocket
}