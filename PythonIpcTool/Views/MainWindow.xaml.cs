using System.ComponentModel; // Required for CancelEventArgs
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using PythonIpcTool.Services;
using PythonIpcTool.ViewModels;

namespace PythonIpcTool.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        InitializeComponent();

        IConfigurationService configService = new JsonConfigurationService();
        IPythonEnvironmentService envService = new PythonEnvironmentService();
        DataContext = new MainViewModel(configService, DialogCoordinator.Instance, envService);

        this.Closing += MainWindow_Closing;
        this.Closed += MainWindow_Closed;
    }

    /// <summary>
    /// Handles the window's Closing event to gracefully terminate any running Python process.
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (this.DataContext is MainViewModel viewModel)
        {
            // --- MODIFIED: Direct and fast cleanup ---
            // Instead of calling the command which might be tied to complex logic,
            // we directly call the cleanup method which now has the fast Kill().
            // This ensures the UI thread is not blocked.
            viewModel.StopPythonProcess();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // When the window is fully closed, dispose of the ViewModel's resources.
        if (this.DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}