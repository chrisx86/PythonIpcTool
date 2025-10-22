using MahApps.Metro.Controls;
using PythonIpcTool.Services;
using PythonIpcTool.ViewModels;
using System.ComponentModel; // Required for CancelEventArgs
using MahApps.Metro.Controls.Dialogs;

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

        // 我們同時提供了 configService 和 DialogCoordinator.Instance
        DataContext = new MainViewModel(configService, DialogCoordinator.Instance);

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
            // MODIFICATION: Call the new StopPythonProcessCommand
            viewModel.StopPythonProcessCommand.Execute(null);
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