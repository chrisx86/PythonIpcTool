namespace PythonIpcTool.ViewModels;

/// <summary>
/// This class provides design-time data for the MainWindow.
/// It inherits from MainViewModel and leverages its design-time-aware constructor.
/// </summary>
public class DesignMainViewModel : MainViewModel
{
    /// <summary>
    /// Initializes a new instance of the DesignMainViewModel class.
    /// This constructor is safe to call from the XAML designer because it
    /// calls the parameterless base constructor of MainViewModel, which handles
    /// the design-time check and data population.
    /// </summary>
    public DesignMainViewModel() : base()
    {
        // The base constructor has already populated the data for design time.
        // You can leave this empty, or add/override more specific data if you want
        // the design-time view to look different from the base MainViewModel's design mode.
        // For example:
        // Log.Information("[DESIGN] Specific data from DesignMainViewModel.");
    }
}

/*
d:DesignInstance: 這是推薦的現代 WPF 設計時數據語法。IsDesignTimeCreatable=True 會告訴設計器去實例化 DesignMainViewModel 類別並將其設置為設計時的 DataContext。
繼承的好處: 因為 DesignMainViewModel 繼承自 MainViewModel，所以它擁有 MainViewModel 的所有屬性和命令。這意味著你在 XAML 中所有的綁定路徑 ({Binding PythonInterpreterPath}, {Binding ExecutePythonScriptCommand}, etc.) 在設計時和運行時都是完全一樣的，這大大簡化了開發和維護。
DesignConfigurationService 提供了獨立於檔案系統的假數據。
DesignMainViewModel 作為一個清晰的入口點，供 XAML 設計器使用。
你的 XAML 設計器（無論是 Visual Studio 還是 Blend）現在應該能正確地顯示預設的路徑、日誌、範例輸出，甚至可以看到 ProgressRing 因為 IsProcessing = true 而顯示出來，這對於調整 UI 佈局和樣式非常有幫助。
 */