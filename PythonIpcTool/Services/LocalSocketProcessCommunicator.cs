using System.IO;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using PythonIpcTool.Models;
using PythonIpcTool.Exceptions;
using Serilog;

namespace PythonIpcTool.Services;

/// <summary>
/// Implements IPC communication with a Python script using a local TCP Socket.
/// C# acts as the server, and the Python script acts as the client.
/// </summary>
public class LocalSocketProcessCommunicator : IPythonProcessCommunicator
{
    private Process? _pythonProcess;
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _internalReadCts;
    private TaskCompletionSource<bool> _processExitedDuringStartup = new TaskCompletionSource<bool>();

    public event Action<string>? OutputReceived;
    public event Action<string>? ErrorReceived;
    public event Action<int>? ProcessExited;

    private volatile bool _isStopping = false;

    public async Task StartProcessAsync(string pythonInterpreterPath, string scriptPath, IpcMode mode, CancellationToken cancellationToken)
    {
        if (mode != IpcMode.StandardIO)
        {
            throw new ArgumentException("StandardIOProcessCommunicator only supports IpcMode.StandardIO");
        }
        StopProcess();
        _internalReadCts = new CancellationTokenSource();

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonInterpreterPath,
            Arguments = $"\"{scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _pythonProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _pythonProcess.Exited += (sender, e) => ProcessExited?.Invoke(_pythonProcess.ExitCode);

        try
        {
            bool started = _pythonProcess.Start();
            if (!started) throw new PythonProcessException("The OS failed to start the Python process.");

            cancellationToken.ThrowIfCancellationRequested();
            // USAGE: Use the internal CancellationToken for background reading tasks
            _ = ReadStreamAsync(_pythonProcess.StandardOutput, OutputReceived, _internalReadCts.Token);
            _ = ReadStreamAsync(_pythonProcess.StandardError, ErrorReceived, _internalReadCts.Token);

            // USAGE: Use the external CancellationToken to allow cancellation during startup delay
            await Task.Delay(100, cancellationToken);

            if (_pythonProcess.HasExited)
            {
                throw new PythonProcessException("Python script did not connect to the socket within the timeout period.");
                //throw new InvalidOperationException($"Python process exited immediately with code {_pythonProcess.ExitCode}.");
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Process start was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write to Python process standard input.");
            throw new PythonProcessException($"Failed to start or connect socket process: {ex.Message}", ex);
        }
    }

    private void OnProcessExitedHandler(object? sender, EventArgs e)
    {
        int exitCode = -1;
        try
        {
            if (_pythonProcess != null) exitCode = _pythonProcess.ExitCode;
        }
        catch { }

        // If the process exits during the critical startup phase, complete the task
        // to signal the failure in StartProcessAsync.
        _processExitedDuringStartup.TrySetResult(true);

        // Always invoke the public event for listeners like the ViewModel
        ProcessExited?.Invoke(exitCode);
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (_pythonProcess == null || _pythonProcess.HasExited)
        {
            throw new InvalidOperationException("Python process is not running.");
        }
        try
        {
            // USAGE: Use WriteLineAsync overload that accepts a CancellationToken
            await _pythonProcess.StandardInput.WriteLineAsync(message.AsMemory(), cancellationToken);
            await _pythonProcess.StandardInput.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("SendMessageAsync was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write to Python process standard input.");
            throw;
        }
    }

    public void StopProcess()
    {
        // --- RE-ENTRANCY GUARD ---
        if (_isStopping)
        {
            return; // Already in the process of stopping, do nothing.
        }
        _isStopping = true;

        _internalReadCts?.Cancel(); // Stop the background readers

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            _pythonProcess.Exited -= OnProcessExitedHandler;
        }

        _stream?.Close();
        _client?.Close();
        _listener?.Stop();

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                Log.Debug("Forcefully terminating Python process.");
                _pythonProcess.Kill(true); // Kill the entire process tree
            }
            catch (InvalidOperationException)
            {
                // This can happen if the process exits between the HasExited check and Kill(), which is fine.
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while stopping Python process.");
            }
        }

        _pythonProcess?.Dispose();
        _pythonProcess = null;
        _internalReadCts?.Dispose();
        _internalReadCts = null;
    }

    private async Task ReadStreamAsync(StreamReader reader, Action<string>? onLineReceived, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // The core logic is the same, but we use the provided reader directly.
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break; // Stream closed

                onLineReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("SendMessageAsync was canceled.");
            throw;
        }
        catch (IOException) { /* Stream was closed, expected */ }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log.Error(ex, "Error reading from standard error stream.");
            }
        }
    }
}