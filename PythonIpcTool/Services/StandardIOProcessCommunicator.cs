// Services/StandardIOProcessCommunicator.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PythonIpcTool.Models; // Ensure this namespace is correct for IpcMode

namespace PythonIpcTool.Services;

/// <summary>
/// Implements IPC communication with a Python script using Standard Input/Output streams.
/// </summary>
public class StandardIOProcessCommunicator : IPythonProcessCommunicator
{
    private Process? _pythonProcess; // The Python process instance
    private CancellationTokenSource? _outputCancellationTokenSource; // Token for reading stdout
    private CancellationTokenSource? _errorCancellationTokenSource;  // Token for reading stderr

    // Events to notify listeners of received output, errors, or process exit
    public event Action<string>? OutputReceived;
    public event Action<string>? ErrorReceived;
    public event Action<int>? ProcessExited;

    /// <summary>
    /// Starts the Python process and begins listening to its standard output and error streams.
    /// 設置 ProcessStartInfo：關鍵是 UseShellExecute = false (必須重定向 I/O) 和 RedirectStandardInput/Output/Error = true。CreateNoWindow = true：防止 Python 腳本彈出黑窗。EnableRaisingEvents = true：允許訂閱 Exited 事件。啟動進程後，ReadStreamAsync 會異步地持續讀取 StandardOutput 和 StandardError，不會阻塞 UI。await Task.Delay(100)：一個小的延遲，讓 Python 進程有時間啟動和初始化，尤其是在低配置機器或複雜腳本啟動時。
    /// </summary>
    /// <param name="pythonInterpreterPath">Path to the Python interpreter executable.</param>
    /// <param name="scriptPath">Path to the Python script to execute.</param>
    /// <param name="mode">The IPC mode (must be StandardIO for this implementation).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if mode is not StandardIO.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the process fails to start.</exception>
    public async Task StartProcessAsync(string pythonInterpreterPath, string scriptPath, IpcMode mode)
    {
        if (mode != IpcMode.StandardIO)
        {
            throw new ArgumentException("StandardIOProcessCommunicator only supports IpcMode.StandardIO");
        }

        // Ensure previous process is stopped before starting a new one
        StopProcess();

        _outputCancellationTokenSource = new CancellationTokenSource();
        _errorCancellationTokenSource = new CancellationTokenSource();

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonInterpreterPath,
            Arguments = scriptPath,
            UseShellExecute = false, // Must be false to redirect I/O
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true, // Do not open a new window for the Python process
            StandardOutputEncoding = Encoding.UTF8, // Specify UTF-8 for consistent encoding
            StandardErrorEncoding = Encoding.UTF8
        };

        _pythonProcess = new Process { StartInfo = startInfo };
        _pythonProcess.EnableRaisingEvents = true; // Essential to subscribe to Exited event
        _pythonProcess.Exited += (sender, e) => ProcessExited?.Invoke(_pythonProcess.ExitCode);

        try
        {
            bool started = _pythonProcess.Start();
            if (!started)
            {
                throw new InvalidOperationException($"Failed to start Python process: {pythonInterpreterPath} {scriptPath}");
            }

            // Start asynchronous reading of StandardOutput and StandardError
            _ = ReadStreamAsync(_pythonProcess.StandardOutput, OutputReceived, _outputCancellationTokenSource.Token);
            _ = ReadStreamAsync(_pythonProcess.StandardError, ErrorReceived, _errorCancellationTokenSource.Token);

            // For robustness, you might want to await for a short period or specific output
            // to confirm the Python script is ready to receive input.
            await Task.Delay(100); // Small delay to allow Python process to initialize
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Failed to start Python process: {ex.Message}");
            StopProcess(); // Clean up if start fails
            throw; // Re-throw to inform the caller (ViewModel)
        }
    }

    /// <summary>
    /// Sends a message to the Python script via its standard input.
    /// 將訊息寫入 _pythonProcess.StandardInput。WriteLineAsync 後跟 FlushAsync 至關重要，確保數據立即發送。
    /// </summary>
    /// <param name="message">The message to send (e.g., a JSON string).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Python process is not running.</exception>
    public async Task SendMessageAsync(string message)
    {
        if (_pythonProcess == null || _pythonProcess.HasExited)
        {
            throw new InvalidOperationException("Python process is not running.");
        }

        try
        {
            // Write the message followed by a newline to signal end of input
            await _pythonProcess.StandardInput.WriteLineAsync(message);
            // Ensure the buffer is flushed immediately
            await _pythonProcess.StandardInput.FlushAsync();
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke($"Failed to write to Python process input: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Stops the running Python process and cleans up resources.
    /// 嘗試先關閉 StandardInput，讓 Python 腳本有機會優雅退出。使用 WaitForExit 等待一段時間，如果 Python 未退出，則使用 Kill() 強制終止。取消 CancellationTokenSource 以停止 ReadStreamAsync 任務，並釋放所有資源。
    /// </summary>
    public void StopProcess()
    {
        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                // Attempt to gracefully close standard input first
                _pythonProcess.StandardInput.Close();
                // Give it a moment to exit naturally
                if (!_pythonProcess.WaitForExit(2000)) // Wait for 2 seconds
                {
                    _pythonProcess.Kill(); // If not exited, force kill
                }
            }
            catch (Exception ex)
            {
                // Log or report error if process killing fails
                ErrorReceived?.Invoke($"Error stopping Python process: {ex.Message}");
            }
            finally
            {
                _outputCancellationTokenSource?.Cancel();
                _errorCancellationTokenSource?.Cancel();
                _outputCancellationTokenSource?.Dispose();
                _errorCancellationTokenSource?.Dispose();
                _outputCancellationTokenSource = null;
                _errorCancellationTokenSource = null;

                _pythonProcess.Dispose();
                _pythonProcess = null;
            }
        }
        else
        {
            _outputCancellationTokenSource?.Cancel();
            _errorCancellationTokenSource?.Cancel();
            _outputCancellationTokenSource?.Dispose();
            _errorCancellationTokenSource?.Dispose();
            _outputCancellationTokenSource = null;
            _errorCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Asynchronously reads lines from a given stream and invokes an action for each line.
    /// 一個通用的異步方法，用於從 StreamReader 讀取每一行。使用 CancellationToken 實現可取消的讀取操作，這對於應用程式關閉或用戶取消操作時停止讀取非常重要。
    /// </summary>
    /// <param name="streamReader">The StreamReader to read from (e.g., StandardOutput or StandardError).</param>
    /// <param name="onLineReceived">The action to invoke when a line is received.</param>
    /// <param name="cancellationToken">Cancellation token to stop reading.</param>
    private async Task ReadStreamAsync(StreamReader streamReader, Action<string>? onLineReceived, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await streamReader.ReadLineAsync(cancellationToken);
                if (line == null) // Stream closed or no more data
                {
                    break;
                }
                if (!string.IsNullOrWhiteSpace(line))
                {
                    onLineReceived?.Invoke(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Task was canceled, expected behavior
        }
        catch (Exception ex)
        {
            // Log or report other reading errors
            ErrorReceived?.Invoke($"Error reading stream: {ex.Message}");
        }
    }
}


/*
    程式碼解釋：
    _pythonProcess： Process 類別的實例，用於控制外部 Python 進程。
    OutputReceived, ErrorReceived, ProcessExited 事件： 這些事件允許 MainViewModel (或其他監聽者) 訂閱來自 Python 進程的輸出、錯誤和退出通知。
    StartProcessAsync：
    設置 ProcessStartInfo：關鍵是 UseShellExecute = false (必須重定向 I/O) 和 RedirectStandardInput/Output/Error = true。
    CreateNoWindow = true：防止 Python 腳本彈出黑窗。
    EnableRaisingEvents = true：允許訂閱 Exited 事件。
    啟動進程後，ReadStreamAsync 會異步地持續讀取 StandardOutput 和 StandardError，不會阻塞 UI。
    await Task.Delay(100)：一個小的延遲，讓 Python 進程有時間啟動和初始化，尤其是在低配置機器或複雜腳本啟動時。
    SendMessageAsync：
    將訊息寫入 _pythonProcess.StandardInput。WriteLineAsync 後跟 FlushAsync 至關重要，確保數據立即發送。
    StopProcess：
    嘗試先關閉 StandardInput，讓 Python 腳本有機會優雅退出。
    使用 WaitForExit 等待一段時間，如果 Python 未退出，則使用 Kill() 強制終止。
    取消 CancellationTokenSource 以停止 ReadStreamAsync 任務，並釋放所有資源。
    ReadStreamAsync：
    一個通用的異步方法，用於從 StreamReader 讀取每一行。
    使用 CancellationToken 實現可取消的讀取操作，這對於應用程式關閉或用戶取消操作時停止讀取非常重要。
*/