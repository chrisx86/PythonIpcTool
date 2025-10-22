using CommunityToolkit.Mvvm.ComponentModel;
using PythonIpcTool.Models;

public partial class ScriptProfile : ObservableObject // 繼承 ObservableObject 以便在 UI 中即時更新
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = "New Profile";

    [ObservableProperty]
    private string _pythonInterpreterPath = "python";

    [ObservableProperty]
    private string _pythonScriptPath = "";

    [ObservableProperty]
    private IpcMode _selectedIpcMode = IpcMode.StandardIO;
}