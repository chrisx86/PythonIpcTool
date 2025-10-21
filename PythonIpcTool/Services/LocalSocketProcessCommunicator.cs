using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PythonIpcTool.Models;
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
            if (!started) throw new InvalidOperationException($"Failed to start Python process.");

            cancellationToken.ThrowIfCancellationRequested();
            // USAGE: Use the internal CancellationToken for background reading tasks
            _ = ReadStreamAsync(_pythonProcess.StandardOutput, OutputReceived, _internalReadCts.Token);
            _ = ReadStreamAsync(_pythonProcess.StandardError, ErrorReceived, _internalReadCts.Token);

            // USAGE: Use the external CancellationToken to allow cancellation during startup delay
            await Task.Delay(100, cancellationToken);

            if (_pythonProcess.HasExited)
            {
                throw new InvalidOperationException($"Python process exited immediately with code {_pythonProcess.ExitCode}.");
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Process start was canceled.");
            StopProcess();
            throw;
        }
        catch (Exception)
        {
            StopProcess();
            throw;
        }
    }

    // MODIFICATION: New private event handler
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
        _internalReadCts?.Cancel(); // Stop the background readers

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            _pythonProcess.Exited -= OnProcessExitedHandler;
        }

        _internalReadCts?.Cancel();

        _stream?.Close();
        _client?.Close();
        _listener?.Stop();

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                Log.Debug("Forcefully terminating Python process.");
                _pythonProcess.Kill(true); // Kill the entire process tree
                //if (!_pythonProcess.WaitForExit(1000))
                //{
                //    Log.Warning("Process did not exit gracefully, killing it.");
                //    _pythonProcess.Kill();
                //}
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

    private async Task ReadStreamAsync(Stream stream, Action<string>? onLineReceived, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                onLineReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("SendMessageAsync was canceled.");
            throw;
        }
        catch (IOException) { /* Socket was closed, expected */ }
        catch (Exception ex)
        {
            Log.Error($"Error reading from stream: {ex.Message}");
        }
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


/*
C# 作為伺服器： TcpListener 啟動一個伺服器，並在一個隨機的可用端口 (port 0) 上監聽。
傳遞端口號： Arguments 現在變為 $"\"{scriptPath}\" socket {port}"，將腳本路徑、socket 關鍵字和伺服器端口號傳遞給 Python 腳本。
等待連接： _listener.AcceptTcpClientAsync() 會異步地等待 Python 客戶端的連接。我們還加入了一個超時機制，防止應用程式因 Python 腳本未能連接而永久掛起。
捕獲 stderr： 即使我們的主要通訊是透過 Socket，RedirectStandardError = true 仍然非常重要。它可以捕獲 Python 腳本在啟動階段的錯誤（例如 import 失敗、語法錯誤或無法連接 Socket），這些錯誤發生在 Socket 通訊建立之前。
SendMessageAsync： 將 JSON 字串和一個換行符 \n 一起編碼為 UTF-8 位元組，並寫入 NetworkStream。
StopProcess： 清理順序很重要。首先關閉網絡資源 (_stream, _client, _listener)，這會讓 Python 腳本的 readline() 返回空，使其能夠優雅退出循環。然後再處理 Process 物件。 
*/