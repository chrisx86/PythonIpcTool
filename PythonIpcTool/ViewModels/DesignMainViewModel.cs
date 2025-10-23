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
        // The base constructor is now correctly called with all required mock dependencies.
        // You can add more design-time specific data here if needed, for example:
        //if (IsInDesignMode)
        //{
        //    // This property is available from ObservableObject (via CommunityToolkit.Mvvm)
        //    // to distinguish design time from run time.
        //    LogEntries.Add(new Models.LogEntry(
        //        new Serilog.Events.LogEvent(
        //            DateTimeOffset.Now,
        //            Serilog.Events.LogEventLevel.Information,
        //            null,
        //            new Serilog.Events.MessageTemplate(new[] { new Serilog.Events.PropertyToken("Design Mode", "Design Mode Active") }),
        //            new System.Collections.Generic.List<Serilog.Events.LogEventProperty>()
        //        )
        //    ));
        //}
    }
}

/*
d:DesignInstance: 這是推薦的現代 WPF 設計時數據語法。IsDesignTimeCreatable=True 會告訴設計器去實例化 DesignMainViewModel 類別並將其設置為設計時的 DataContext。
繼承的好處: 因為 DesignMainViewModel 繼承自 MainViewModel，所以它擁有 MainViewModel 的所有屬性和命令。這意味著你在 XAML 中所有的綁定路徑 ({Binding PythonInterpreterPath}, {Binding ExecutePythonScriptCommand}, etc.) 在設計時和運行時都是完全一樣的，這大大簡化了開發和維護。
DesignConfigurationService 提供了獨立於檔案系統的假數據。
DesignMainViewModel 作為一個清晰的入口點，供 XAML 設計器使用。
你的 XAML 設計器（無論是 Visual Studio 還是 Blend）現在應該能正確地顯示預設的路徑、日誌、範例輸出，甚至可以看到 ProgressRing 因為 IsProcessing = true 而顯示出來，這對於調整 UI 佈局和樣式非常有幫助。
*/