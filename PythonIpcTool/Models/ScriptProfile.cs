using CommunityToolkit.Mvvm.ComponentModel;
using PythonIpcTool.Models;

public partial class ScriptProfile : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "New Profile";

    [ObservableProperty]
    private string _pythonInterpreterPath = @"C:\Python310\python.exe";

    [ObservableProperty]
    private string _pythonScriptPath = @"E:\Project\VisualStudio\PythonIpcTool\PythonIpcTool\PythonScripts\simple_processor.py";

    [ObservableProperty]
    private IpcMode _selectedIpcMode = IpcMode.StandardIO;

    [ObservableProperty]
    private string _inputData = "{\"value\": \"Hello from C#\", \"numbers\": [1, 2, 3]}";
}