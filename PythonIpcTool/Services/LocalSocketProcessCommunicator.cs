using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using PythonIpcTool.Models;

namespace PythonIpcTool.Services
{
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
        private CancellationTokenSource? _cancellationTokenSource;
        private TaskCompletionSource<bool> _processExitedDuringStartup = new TaskCompletionSource<bool>();

        public event Action<string>? OutputReceived;
        public event Action<string>? ErrorReceived;
        public event Action<int>? ProcessExited;

        public async Task StartProcessAsync(string pythonInterpreterPath, string scriptPath, IpcMode mode)
        {
            if (mode != IpcMode.LocalSocket)
            {
                throw new ArgumentException("LocalSocketProcessCommunicator only supports IpcMode.LocalSocket");
            }
            StopProcess();

            _cancellationTokenSource = new CancellationTokenSource();
            _processExitedDuringStartup = new TaskCompletionSource<bool>(); // Reset for each start

            // 1. Start TCP listener on a free port
            _listener = new TcpListener(IPAddress.Loopback, 0); // Port 0 lets the OS pick a free port
            _listener.Start();
            int port = ((IPEndPoint)_listener.Server.LocalEndPoint!).Port;

            // 2. Configure Python process to connect to our socket
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonInterpreterPath,
                // Pass 'socket' and the port number as arguments to the Python script
                Arguments = $"\"{scriptPath}\" socket {port}",
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true, // Still capture standard error for script startup issues
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8
            };

            _pythonProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _pythonProcess.Exited += OnProcessExitedHandler;
            _pythonProcess.Exited += (sender, e) => ProcessExited?.Invoke(_pythonProcess.ExitCode);

            try
            {
                bool started = _pythonProcess.Start();
                if (!started) throw new InvalidOperationException("Failed to start Python process.");

                // 3. Asynchronously wait for the Python client to connect
                var acceptValueTask = _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                var timeoutTask = Task.Delay(5000, _cancellationTokenSource.Token); // 5-second timeout
                var acceptTask = acceptValueTask.AsTask();
                var completedTask = await Task.WhenAny(acceptTask, timeoutTask, _processExitedDuringStartup.Task);

                if (completedTask == acceptTask)
                {
                    _client = await acceptTask;
                    _stream = _client.GetStream();

                    // Start listening for messages from Python and for stderr
                    _ = ReadStreamAsync(_stream, OutputReceived, _cancellationTokenSource.Token);

                    // --- CORRECTION START ---
                    // The original code passed `_pythonProcess.StandardError` which is a `StreamReader`.
                    // The `ReadStreamAsync` method expects a `Stream`.
                    // We get the underlying stream by using the `BaseStream` property.
                    _ = ReadStreamAsync(_pythonProcess.StandardError.BaseStream, ErrorReceived, _cancellationTokenSource.Token);
                    // --- CORRECTION END ---
                }
                else
                {
                    throw new TimeoutException("Python script did not connect to the socket within the timeout period.");
                }
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Failed to start or connect socket process: {ex.Message}");
                StopProcess(); // Clean up on failure
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

        public async Task SendMessageAsync(string message)
        {
            if (_stream == null || !_stream.CanWrite)
            {
                throw new InvalidOperationException("Socket stream is not available or has been closed.");
            }

            try
            {
                // Append a newline character to mark the end of the message
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                await _stream.WriteAsync(data, 0, data.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);
                await _stream.FlushAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Failed to send message via socket: {ex.Message}");
                throw;
            }
        }

        public void StopProcess()
        {
            if (_pythonProcess != null)
            {
                _pythonProcess.Exited -= OnProcessExitedHandler;
            }

            _cancellationTokenSource?.Cancel();

            _stream?.Close();
            _client?.Close();
            _listener?.Stop();

            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                try
                {
                    if (!_pythonProcess.WaitForExit(2000))
                    {
                        _pythonProcess.Kill();
                    }
                }
                catch (Exception ex)
                {
                    ErrorReceived?.Invoke($"Error stopping Python process: {ex.Message}");
                }
            }

            _pythonProcess?.Dispose();
            _pythonProcess = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
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
            catch (OperationCanceledException) { /* Expected */ }
            catch (IOException) { /* Socket was closed, expected */ }
            catch (Exception ex)
            {
                ErrorReceived?.Invoke($"Error reading from stream: {ex.Message}");
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