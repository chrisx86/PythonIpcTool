using PythonIpcTool.Services;

namespace PythonIpcTool.ViewModels;

public class DesignMainViewModel : MainViewModel
{
    /// <summary>
    /// Initializes a new instance of the DesignMainViewModel class.
    /// It provides design-time safe mock services to the base MainViewModel constructor.
    /// </summary>
    public DesignMainViewModel()
        : base(new DesignConfigurationService(), new DesignDialogCoordinator(), new DesignPythonEnvironmentService())
    {

    }
}
