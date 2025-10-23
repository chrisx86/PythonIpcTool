using System.IO;
using System.Text;
using System.Diagnostics;
using PythonIpcTool.Models;
using PythonIpcTool.Exceptions;
using Serilog; // Ensure this namespace is correct for IpcMode

namespace PythonIpcTool.Services;

/// <summary>
/// Implements IPC communication with a Python script using Standard Input/Output streams.
/// </summary>
public class StandardIOProcessCommunicator : IPythonProcessCommunicator
{
    private Process? _pythonProcess; // The Python process instance
    private CancellationTokenSource? _internalReadCts;

    // Events to notify listeners of received output, errors, or process exit
    public event Action<string>? OutputReceived;
    public event Action<string>? ErrorReceived;
    public event Action<int>? ProcessExited;

    private volatile bool _isStopping = false;

    /// <summary>
    /// Starts the Python process and begins listening to its standard output and error streams.
    /// 設置 ProcessStartInfo：關鍵是 UseShellExecute = false (必須重定向 I/O) 和 RedirectStandardInput/Output/Error = true。CreateNoWindow = true：防止 Python 腳本彈出黑窗。EnableRaisingEvents = true：允許訂閱 Exited 事件。啟動進程後，ReadStreamAsync 會異步地持續讀取 StandardOutput 和 StandardError，不會阻塞 UI。await Task.Delay(100)：一個小的延遲，讓 Python 進程有時間啟動和初始化，尤其是在低配置機器或複雜腳本啟動時。
    /// </summary>
    /// <param name="pythonInterpreterPath">Path to the Python interpreter executable.</param>
    /// <param name="argumentsOrScriptPath">Path to the Python script to execute.</param>
    /// <param name="mode">The IPC mode (must be StandardIO for this implementation).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if mode is not StandardIO.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the process fails to start.</exception>
    public async Task StartProcessAsync(string pythonInterpreterPath, string argumentsOrScriptPath, IpcMode mode, CancellationToken cancellationToken)
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
            Arguments = argumentsOrScriptPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _pythonProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _pythonProcess.Exited += (sender, e) => ProcessExited?.Invoke(_pythonProcess?.ExitCode ?? -1);

        try
        {
            cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before starting

            bool started = _pythonProcess.Start();
            if (!started)
            {
                throw new InvalidOperationException($"Failed to start Python process: {pythonInterpreterPath} {argumentsOrScriptPath}");
            }

            // Start background reading tasks with an internal token so they can be stopped separately
            _ = ReadStreamAsync(_pythonProcess.StandardOutput, OutputReceived, _internalReadCts.Token);
            _ = ReadStreamAsync(_pythonProcess.StandardError, ErrorReceived, _internalReadCts.Token);

            // USAGE: Use the external cancellationToken for the startup delay.
            // This allows the user's "Cancel" action to interrupt the startup process.
            await Task.Delay(100, cancellationToken);

            if (_pythonProcess.HasExited)
            {
                throw new InvalidOperationException($"Python process exited immediately with code {_pythonProcess.ExitCode}. Check logs for errors.");
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Process start was canceled by the user.");
            throw; // Re-throw so the ViewModel knows it was canceled
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write to Python process standard input.");
            throw new PythonProcessException($"Failed to start or connect socket process: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Sends a message to the Python script via its standard input.
    /// 將訊息寫入 _pythonProcess.StandardInput。WriteLineAsync 後跟 FlushAsync 至關重要，確保數據立即發送。
    /// </summary>
    /// <param name="message">The message to send (e.g., a JSON string).</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Python process is not running.</exception>
    public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (_pythonProcess == null || _pythonProcess.HasExited)
        {
            throw new InvalidOperationException("Python process is not running.");
        }
        try
        {
            // USAGE: Use the WriteLineAsync overload that accepts a CancellationToken.
            // .AsMemory() is an efficient way to pass the string data.
            await _pythonProcess.StandardInput.WriteLineAsync(message.AsMemory(), cancellationToken);
            await _pythonProcess.StandardInput.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Warning("SendMessageAsync was canceled by the user.");
            throw; // Re-throw to inform the ViewModel
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to write to Python process standard input.");
            throw new PythonProcessException($"Failed to start or connect socket process: {ex.Message}", ex);
        }
        finally
        {
            // --- CRUCIAL MODIFICATION ---
            // Close the standard input stream after writing.
            // This sends the End-of-File (EOF) signal to the Python script,
            // which allows sys.stdin.read() to complete.
            _pythonProcess.StandardInput.Close();
        }
    }

    /// <summary>
    /// Stops the running Python process and cleans up resources.
    /// 嘗試先關閉 StandardInput，讓 Python 腳本有機會優雅退出。使用 WaitForExit 等待一段時間，如果 Python 未退出，則使用 Kill() 強制終止。取消 CancellationTokenSource 以停止 ReadStreamAsync 任務，並釋放所有資源。
    /// </summary>
    public void StopProcess()
    {
        if (_isStopping)
        {
            return; // Already in the process of stopping, do nothing.
        }
        _isStopping = true;

        _internalReadCts?.Cancel(); // Stop the background reading tasks

        if (_pythonProcess != null && !_pythonProcess.HasExited)
        {
            try
            {
                Log.Debug("Forcefully terminating Python process.");
                _pythonProcess.Kill(true); // Kill the entire process tree
                //if (!_pythonProcess.WaitForExit(1000))
                //{
                //    Log.Warning("Process did not exit gracefully, killing it.");
                //    _pythonProcess.Kill(true); // Kill the entire process tree
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while stopping Python process.");
                throw new PythonProcessException($"Failed to while stopping Python process: {ex.Message}", ex);
            }
        }

        _pythonProcess?.Dispose();
        _pythonProcess = null;
        _internalReadCts?.Dispose();
        _internalReadCts = null;
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
            Log.Warning("SendMessageAsync was canceled.");
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                Log.Error(ex, "Error reading from standard I/O stream.");
            }
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