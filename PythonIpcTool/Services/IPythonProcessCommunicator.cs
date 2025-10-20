using PythonIpcTool.Models;

namespace PythonIpcTool.Services;

public interface IPythonProcessCommunicator
{
    event Action<string> OutputReceived;
    event Action<string> ErrorReceived;
    event Action<int> ProcessExited;

    Task StartProcessAsync(string pythonInterpreterPath, string scriptPath, IpcMode mode);
    Task SendMessageAsync(string message);
    void StopProcess();
}