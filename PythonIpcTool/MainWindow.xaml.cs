using MahApps.Metro.Controls;
using PythonIpcTool.Services;
using PythonIpcTool.ViewModels;
using System.ComponentModel; // Required for CancelEventArgs

namespace PythonIpcTool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Create an instance of the StandardIOProcessCommunicator.
            // NOTE: This direct instantiation is a point of tight coupling and can be
            // improved later with a factory or a Dependency Injection container.
            IPythonProcessCommunicator ipcCommunicator = new StandardIOProcessCommunicator();

            // Instantiate MainViewModel and set it as the DataContext for this window.
            this.DataContext = new MainViewModel();

            // Register the Closing event to ensure proper resource cleanup.
            this.Closing += MainWindow_Closing;
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
    }
}