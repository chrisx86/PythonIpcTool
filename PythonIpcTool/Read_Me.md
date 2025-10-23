## 系統需求書 - Python 互動式 IPC 工具 (WPF C#)

### 1. 專案概述

**1.1 專案名稱：** Python 互動式 IPC 工具 (WPF C#)

**1.2 專案主題：** 開發一個可以與 Python 腳本互動的行程間通訊 (IPC) 工具

**1.3 專案描述：**
本專案旨在開發一個基於 WPF (C#) 的桌面應用程式，用於實現與外部 Python 腳本的雙向行程間通訊 (IPC)。在現代軟體開發中，許多複雜的數據分析、機器學習模型或特定任務功能常使用 Python 腳本實現。本工具將提供一個使用者友善且高效的介面，接收使用者輸入，然後透過標準輸入/輸出 (Standard I/O) 或本地 Socket 將資料傳遞給指定的 Python 腳本進行處理。Python 腳本完成運算後，其結果將被 WPF 應用程式接收並以清晰、現代、具結構化的方式呈現給使用者。本專案特別注重系統的模組化、可擴充性、安全性及使用者體驗。

**1.4 學習重點：**
*   **行程間通訊 (IPC)：** 深入學習如何使用 `System.Diagnostics.Process` 類別啟動外部程式，並有效地重定向其 `StandardInput`、`StandardOutput` 和 `StandardError`。同時，探索 `System.Net.Sockets` 實現更靈活的 Socket 通訊機制。
*   **資料交換格式：** 掌握使用 JSON 作為 C# 和 Python 之間資料交換的首選格式，學習兩種語言的序列化與反序列化技術，確保資料傳輸的結構化與可靠性。
*   **整合異質系統：** 理解並實踐如何將不同程式語言（C# 和 Python）的功能模組無縫整合到一個應用程式中，這是一種在現代軟體開發中常用且實用的跨語言通訊模式。
*   **WPF 現代化開發：** 應用 MVVM 設計模式、異步編程 (`async`/`await`) 和現代 UI 元件庫 (如 MahApps.Metro, Material Design) 來構建響應式、高效能且美觀的 WPF 應用程式。

### 2. 功能需求 (Functional Requirements)

**2.1 使用者介面 (UI) 功能：**

*   **Python 環境設定區：**
    *   **Python 解釋器路徑：** 提供文字輸入框和瀏覽按鈕，供使用者指定 Python 解釋器 (例如 `python.exe` 或虛擬環境中的解釋器) 的完整路徑。
    *   **Python 腳本路徑：** 提供文字輸入框和瀏覽按鈕，供使用者指定要執行之 Python 腳本檔案的完整路徑。
    *   **虛擬環境支援：** 應用程式應能自動檢測並優先使用指定的 Python 虛擬環境中的解釋器。
    *   **IPC 模式選擇：** 提供兩個單選按鈕 (RadioButton)，供使用者選擇使用 Standard I/O 或本地 Socket 進行 IPC 通訊。
    *   **配置保存：** 上述設定應能自動保存並在應用程式下次啟動時自動載入。
*   **輸入資料區域 (Input Data Area)：**
    *   提供一個多行文字輸入框 (TextBox)，供使用者輸入欲傳遞給 Python 腳本的 JSON 格式資料。
*   **操作控制區 (Action Control Area)：**
    *   **執行按鈕：** 提供一個明確的按鈕（例如 "執行 Python 腳本"），觸發資料序列化、傳輸和 Python 腳本執行。
    *   **取消按鈕：** 對於長時間運行的腳本，提供一個 "取消" 按鈕，允許使用者中止當前 Python 腳本的執行。
    *   **清除輸入：** 提供一個 "清除輸入" 按鈕，清空輸入區域內容。
*   **結果顯示區域 (Output Data Area)：**
    *   提供一個多行文字顯示區 (TextBox/TextBlock)，用於顯示 Python 腳本返回的 JSON 格式結果。
    *   顯示區應具備基本的語法高亮或格式化功能，以提升 JSON 資料的可讀性。
*   **狀態與日誌顯示區域 (Status & Log Area)：**
    *   提供一個滾動式文字顯示區，顯示應用程式的運行狀態（例如 "正在執行...", "執行完成", "等待輸入..."）。
    *   顯示來自 Python 腳本的 `StandardError` 輸出。
    *   顯示應用程式內部日誌訊息（例如配置載入、IPC 通訊細節、警告、錯誤）。
    *   應區分不同日誌級別的訊息（例如 INFO, WARN, ERROR）。

**2.2 核心邏輯功能：**

*   **Python 進程管理：**
    *   能夠透過 `System.Diagnostics.Process` 啟動指定的 Python 解釋器，將目標 Python 腳本作為第一個參數傳遞。
    *   正確配置 `ProcessStartInfo`，重定向 `StandardInput`、`StandardOutput` 和 `StandardError` 以實現 Standard I/O 通訊。
    *   能夠透過 Socket 建立雙向通訊管道，啟動 Python 進程並告知其 Socket 連接資訊。
    *   提供機制在使用者取消或應用程式關閉時，可靠地終止 Python 進程。
*   **資料序列化與反序列化：**
    *   將使用者輸入的 JSON 字串直接傳遞給 Python 腳本。
    *   Python 腳本返回的結果應為 JSON 格式字串。C# 應用程式負責接收並顯示。
*   **資料傳輸 (C# -> Python)：**
    *   **Standard I/O 模式：** 將 JSON 資料寫入重定向的 `StandardInput` 流。
    *   **本地 Socket 模式：** 建立本地 TCP Socket 連接，將 JSON 資料透過 Socket 發送給 Python 腳本。
*   **資料接收 (Python -> C#)：**
    *   **Standard I/O 模式：** 異步監聽 Python 腳本的 `StandardOutput` 流，逐行或逐段讀取結果。
    *   **本地 Socket 模式：** 異步監聽 Socket 連接，接收 Python 腳本發送的 JSON 資料。
*   **錯誤處理與異常捕獲：**
    *   捕獲 Python 腳本執行過程中的錯誤（例如腳本語法錯誤、運行時異常），並將 `StandardError` 的內容解析為結構化錯誤訊息，顯示給使用者。
    *   處理 IPC 通訊過程中的異常（例如 Python 進程未啟動、Socket 連接失敗、資料解析錯誤）。
    *   提供友善的錯誤提示，並將詳細錯誤記錄到日誌系統。
*   **IPC 模式切換：** 根據使用者選擇，動態啟用 Standard I/O 或 Socket 模式的通訊邏輯。
*   **配置管理：** 使用獨立的配置服務 (`IConfigurationService`) 負責應用程式設定 (Python 路徑、腳本路徑、IPC 模式等) 的保存和載入。建議使用 `appsettings.json` 或 `User.config` 進行儲存。

**2.3 Python 腳本功能 (範例)：**

*   **接收輸入：** Python 腳本能夠從 `sys.stdin` 或本地 Socket 接收 C# 應用程式傳來的 JSON 資料。
*   **處理資料：** 對接收到的 JSON 資料執行邏輯處理（例如，解析 JSON 物件、執行計算、調用機器學習模型等）。
*   **返回結果：** 將處理結果序列化為 JSON 格式字串，並透過 `sys.stdout` 或本地 Socket 返回給 C# 應用程式。
*   **錯誤輸出：** 腳本在遇到異常時，應將錯誤訊息輸出到 `sys.stderr`。

### 3. 非功能性需求 (Non-Functional Requirements)

**3.1 效能 (Performance)：**
*   IPC 通訊應在合理的時間內完成，即使傳輸較大的資料量（例如 MB 級別的 JSON）。
*   UI 應保持高度響應性，避免在等待 Python 腳本執行時出現凍結。所有 IPC 操作應使用異步 (`async`/`await`) 編程，確保 UI 線程的流暢。
*   針對長時運行的 Python 腳本，UI 應提供進度指示，且不應影響應用程式的其他互動。

**3.2 可用性 (Usability)：**
*   直觀且易於理解的使用者介面，使用者能夠輕鬆進行設定、輸入資料、執行腳本並查看結果。
*   清晰的結果顯示，特別是 JSON 資料的格式化和語法高亮。
*   友善的狀態提示、載入動畫、空資料顯示和錯誤訊息，引導使用者操作和診斷問題。
*   支援鍵盤導航和基本觸控操作。

**3.3 可維護性 (Maintainability)：**
*   程式碼應嚴格遵循 MVVM 設計模式，實現邏輯與 UI 的高度分離。
*   C# 和 Python 腳本的程式碼應結構清晰、模組化，並有全面且準確的英文註解。
*   各模組（IPC 服務、資料序列化服務、配置服務）應遵循單一職責原則。
*   易於替換、更新或擴展 Python 腳本，無需修改 C# 應用程式的核心邏輯。

**3.4 擴充性 (Extensibility)：**
*   **IPC 抽象化：** 核心 IPC 邏輯應抽象為介面 (`IProcessCommunicator`)，支持未來引入更多 IPC 方式（如 Named Pipes），而無需重構應用程式的核心業務邏輯。
*   **多腳本支援：** 考慮未來可擴展為支持多個預定義 Python 腳本的選擇，每個腳本可有獨立的配置。
*   **資料模型靈活性：** 選擇 JSON 作為資料格式，確保未來能夠輕鬆擴充傳輸更複雜的資料結構。
*   **日誌擴展：** 具備可插拔的日誌輸出方式（例如檔案、資料庫、遠端服務）。

**3.5 錯誤處理與健壯性 (Error Handling & Robustness)：**
*   程式應能妥善處理各種異常情況，如 Python 解釋器路徑錯誤、腳本不存在、IPC 通訊失敗、無效 JSON 輸入/輸出、Python 進程非正常終止等。
*   提供有意義、易於理解的錯誤訊息，幫助使用者診斷問題。
*   應用程式應能從錯誤中恢復或提供清晰的退出機制，避免崩潰。

**3.6 UI 設計 (UI Design)：**
*   採用現代 UI 設計風格，如 Fluent Design System 或 Material Design in XAML，結合 MahApps.Metro 或 Material Design in XAML 元件庫，提升使用者體驗。
*   確保介面佈局清晰、間距合理、配色協調，遵循視覺層次原則。
*   支援基本響應式佈局，適應不同視窗大小，並考慮高 DPI 顯示。
*   提供 Dark Mode / Light Mode 自動切換機制。

**3.7 安全性 (Security)：**
*   **Python 環境隔離：** 優先使用 Python 虛擬環境，避免系統環境污染。
*   **腳本執行限制 (高階)：** 考慮未來增加腳本執行沙盒或白名單機制，限制 Python 腳本可訪問的系統資源，以防範惡意腳本執行。

### 4. 技術棧 (Technology Stack)

**4.1 前端 (WPF C#)：**
*   **語言：** C#
*   **框架：** .NET (WPF), .NET 8 或最新 LTS 版本
*   **設計模式：** MVVM (建議使用 `CommunityToolkit.Mvvm` 或 `Prism`)
*   **UI 元件庫 (建議)：** MahApps.Metro 或 Material Design in XAML
*   **IPC 抽象化：** 自定義介面 `IProcessCommunicator` 及其實現 (`StandardIOProcessCommunicator`, `SocketProcessCommunicator`)
*   **IPC (Standard I/O)：** `System.Diagnostics.Process`
*   **IPC (Socket)：** `System.Net.Sockets`
*   **資料序列化：** `System.Text.Json`
*   **日誌：** NLog 或 Serilog

**4.2 後端 (Python)：**
*   **語言：** Python 3.x (建議使用最新版本)
*   **IPC (Standard I/O)：** `sys.stdin`, `sys.stdout`, `sys.stderr`
*   **IPC (Socket)：** `socket` 模組
*   **資料序列化/反序列化：** `json` 模組
*   **環境管理：** `venv` 或 `conda` (建議在腳本層面考慮)

### 5. 開發環境 (Development Environment)

*   **IDE：** Visual Studio (C#), Visual Studio Code / PyCharm (Python)
*   **作業系統：** Windows 10/11
*   **.NET SDK：** 最新版本
*   **Python 環境：** Python 3.x (建議使用虛擬環境)

### 6. 測試計畫 (Test Plan)

*   **單元測試：**
    *   對 ViewModel、服務層 (IPC 服務、配置服務、序列化服務) 的核心邏輯進行單元測試。
    *   測試資料序列化和反序列化功能。
*   **整合測試：**
    *   測試 C# 應用程式與 Python 腳本之間在 Standard I/O 模式下的 IPC 通訊是否正常工作，包括不同資料量和錯誤情況。
    *   測試 C# 應用程式與 Python 腳本之間在本地 Socket 模式下的 IPC 通訊是否正常工作，包括不同資料量和錯誤情況。
    *   測試配置的保存和載入。
    *   測試進程啟動、取消和終止。
*   **UI 測試：**
    *   確保 UI 元素正確顯示，佈局響應式。
    *   驗證使用者互動流暢，狀態提示、載入動畫和錯誤訊息顯示正確。
    *   測試 Dark Mode / Light Mode 切換。
*   **錯誤處理測試：**
    *   模擬各種錯誤情況：Python 解釋器路徑錯誤、腳本不存在、腳本執行時拋出異常、IPC 通訊異常、無效 JSON 輸入/輸出。
    *   驗證錯誤訊息的準確性和日誌記錄的完整性。
*   **效能測試：**
    *   測試傳輸不同大小 (KB, MB) 的 JSON 資料時的 IPC 響應時間和 UI 流暢度。
    *   測試長時間運行腳本時的資源佔用和穩定性。
*   **安全測試 (高階)：** 評估執行外部腳本的潛在安全漏洞。

---

這份系統需求書現在更加全面和詳細，特別強調了模組化、可擴充性和使用者體驗，為專案的開發提供了一個清晰且強大的藍圖。

現在，您希望從哪個部分開始深入討論和設計呢？例如，我們可以從 MVVM 基礎架構、IPC 抽象層的程式碼設計，或 UI 的 XAML 佈局和風格設定開始。


1.  **定義基礎資料模型**
2.  **配置 App.xaml**

這兩個步驟是任何 WPF 應用程式的基礎，特別是當我們希望應用程式有清晰的結構和現代化的 UI 時。

---

### **步驟 5. 定義基礎資料模型 (Models)**

**目標：** 建立用於在 C# 應用程式內部和 C# 與 Python 腳本之間交換資料的類別。這些類別將代表 JSON 格式的數據結構。

**為什麼這樣做：**
*   **數據結構化：** 將輸入和輸出數據封裝到明確定義的類別中，提高程式碼的可讀性和可維護性。
*   **類型安全：** C# 作為強類型語言，使用類別來表示數據可以利用編譯時檢查，減少運行時錯誤。
*   **JSON 序列化/反序列化：** .NET 的 `System.Text.Json` 或 `Newtonsoft.Json` 可以輕鬆地將這些類別實例序列化為 JSON 字串，或將 JSON 字串反序列化為類別實例。這對於與 Python 進行數據交換至關重要。
*   **MVVM 基礎：** 這些 Model 類別將被 ViewModel 使用，作為數據層的表示。

**實作細節：**

1.  **在 `Models` 資料夾中建立檔案：**
    *   在專案根目錄下建立一個名為 `Models` 的資料夾。
    *   在這個資料夾中，建立兩個新的 C# 類別檔案：`PythonInputData.cs` 和 `PythonOutputResult.cs`。

2.  **`PythonInputData.cs` - 定義輸入資料模型：**
    這個類別將代表從 C# 應用程式發送到 Python 腳本的數據結構。

    ```csharp
    // Models/PythonInputData.cs
    using System.Text.Json.Serialization; // For JSON serialization attributes

    /// <summary>
    /// Represents the input data structure sent from C# to the Python script.
    /// This model will be serialized into a JSON string.
    /// </summary>
    public class PythonInputData
    {
        /// <summary>
        /// A generic string value for the Python script to process.
        /// Can be adapted based on specific script requirements.
        /// </summary>
        [JsonPropertyName("value")] // Maps to "value" key in JSON
        public string? Value { get; set; }

        /// <summary>
        /// An optional list of numbers for numerical processing in Python.
        /// </summary>
        [JsonPropertyName("numbers")] // Maps to "numbers" key in JSON
        public List<double>? Numbers { get; set; }

        /// <summary>
        /// An optional custom payload for more complex data.
        /// This could be any JSON-serializable object.
        /// </summary>
        [JsonPropertyName("customPayload")] // Maps to "customPayload" key in JSON
        public object? CustomPayload { get; set; }

        // You can add more properties here as per your Python script's input requirements.
        // For example:
        // [JsonPropertyName("command")]
        // public string? Command { get; set; }
    }
    ```

    *   **`[JsonPropertyName("name")]`：** 這是 `System.Text.Json` 命名空間中的一個屬性，用於指定在 JSON 序列化/反序列化時，C# 屬性應對應的 JSON 鍵名。這對於確保 C# 和 Python 之間的 JSON 鍵名一致性非常重要。
    *   **屬性類型：** 根據 Python 腳本預期的數據類型選擇合適的 C# 類型。這裡提供了 `string` 和 `List<double>` 的範例。
    *   **`?` (Nullable Reference Types)：** 使用 `?` 表示屬性可以是 `null`，這有助於編譯時空值檢查，提高程式碼健壯性。

3.  **`PythonOutputResult.cs` - 定義輸出結果模型：**
    這個類別將代表從 Python 腳本返回給 C# 應用程式的數據結構。

    ```csharp
    // Models/PythonOutputResult.cs
    using System.Text.Json.Serialization; // For JSON serialization attributes

    /// <summary>
    /// Represents the output data structure received from the Python script.
    /// This model will be deserialized from a JSON string.
    /// </summary>
    public class PythonOutputResult
    {
        /// <summary>
        /// The main processing result from the Python script.
        /// </summary>
        [JsonPropertyName("result")] // Maps to "result" key in JSON
        public string? Result { get; set; }

        /// <summary>
        /// The status of the operation (e.g., "success", "failure", "processing").
        /// </summary>
        [JsonPropertyName("status")] // Maps to "status" key in JSON
        public string? Status { get; set; }

        /// <summary>
        /// An optional error message if the Python script encountered an issue.
        /// </summary>
        [JsonPropertyName("error")] // Maps to "error" key in JSON
        public string? Error { get; set; }

        /// <summary>
        /// An optional custom data object for more complex return data.
        /// </summary>
        [JsonPropertyName("customData")] // Maps to "customData" key in JSON
        public object? CustomData { get; set; }

        /// <summary>
        /// Indicates if the operation was successful.
        /// </summary>
        [JsonIgnore] // This property will not be serialized/deserialized to/from JSON
        public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(Error);

        // You can add more properties here as per your Python script's output requirements.
    }
    ```

    *   `IsSuccess` 屬性被標記為 `[JsonIgnore]`，表示這個屬性僅用於 C# 內部邏輯，不會參與 JSON 的序列化或反序列化。這是一個很好的實踐，用於添加 UI 或業務邏輯相關的派生屬性。
    *   `Status` 和 `Error` 屬性對於處理 Python 腳本執行狀態非常有用。

4.  **定義 IPC 模式列舉 (Enum)：**
    這個列舉用於明確指出 IPC 通訊的方式。

    ```csharp
    // Models/IpcMode.cs (or could be in Services folder)
    /// <summary>
    /// Defines the inter-process communication (IPC) modes available.
    /// </summary>
    public enum IpcMode
    {
        /// <summary>
        /// Communication via Standard Input/Output streams.
        /// </summary>
        StandardIO,
        /// <summary>
        /// Communication via local TCP Sockets.
        /// </summary>
        LocalSocket
    }
    ```
    *   這個 `enum` 可以放在 `Models` 資料夾，也可以放在 `Services` 資料夾，取決於您認為它更偏向數據模型還是服務行為。為了方便起見，初期放在 `Models` 也可。

---

### **步驟 6. 配置 App.xaml：**

**目標：** 配置 WPF 應用程式的全局資源，包括引入 UI 元件庫的主題、樣式和定義應用程式級別的樣式，為統一的 UI 外觀打下基礎。

**為什麼這樣做：**
*   **全局樣式：** `App.xaml` 是應用程式的入口點，配置在其中的資源 (`ResourceDictionary`) 會被整個應用程式的所有窗口和控制項繼承。
*   **UI 一致性：** 引入 UI 元件庫 (如 MahApps.Metro 或 Material Design) 的資源，可以讓應用程式快速擁有現代且一致的設計風格。
*   **主題管理：** 方便管理應用程式的 Light/Dark Mode 或其他自定義主題。

**實作細節：**

1.  **打開 `App.xaml`：**
    在您的 WPF 專案中找到 `App.xaml` 檔案。

2.  **引入 UI 元件庫資源字典 (以 MahApps.Metro 為例)：**
    在 `<Application.Resources>` 標籤中添加 `ResourceDictionary`，引入 MahApps.Metro 的主題和控制項樣式。如果您選擇 Material Design in XAML，則引入 Material Design 的資源。

    ```xml
    <!-- App.xaml -->
    <Application x:Class="PythonIpcTool.App"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:local="clr-namespace:PythonIpcTool"
                 StartupUri="Views/MainWindow.xaml">
        <Application.Resources>
            <ResourceDictionary>
                <ResourceDictionary.MergedDictionaries>
                    <!-- MahApps.Metro resource dictionaries. Make sure to download these via NuGet. -->
                    <!-- 
                        These dictionaries define the base theme (light/dark) and accent color.
                        You can choose different themes and accents as needed.
                        For example, "Light.Red" or "Dark.Blue".
                    -->
                    <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml" />
                    <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml" />
                    <ResourceDictionary Source="pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml" />
                    <!-- If you want to use Material Design:
                    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml" />
                    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml" />
                    <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.DeepPurple.xaml" />
                    <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Lime.xaml" />
                    -->

                    <!-- Define application-specific styles or resources here -->
                    <!-- Example: A global style for TextBoxes -->
                    <Style TargetType="TextBox">
                        <Setter Property="Padding" Value="5"/>
                        <Setter Property="BorderBrush" Value="{DynamicResource MahApps.Brushes.Accent}"/>
                        <Setter Property="BorderThickness" Value="1"/>
                        <Setter Property="VerticalContentAlignment" Value="Center"/>
                        <Setter Property="FontSize" Value="14"/>
                    </Style>

                    <!-- Example: A global style for Buttons -->
                    <Style TargetType="Button">
                        <Setter Property="Padding" Value="10 5"/>
                        <Setter Property="FontSize" Value="14"/>
                        <Setter Property="Background" Value="{DynamicResource MahApps.Brushes.Accent}"/>
                        <Setter Property="Foreground" Value="{DynamicResource MahApps.Brushes.ThemeBackground}"/>
                        <Setter Property="BorderBrush" Value="{DynamicResource MahApps.Brushes.Accent}"/>
                        <Setter Property="BorderThickness" Value="1"/>
                        <Setter Property="Margin" Value="5"/>
                    </Style>

                </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
        </Application.Resources>
    </Application>
    ```

    *   **`pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml`：** 引入 MahApps.Metro 的基礎控制項樣式。
    *   **`pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Blue.xaml`：** 設置應用程式的主題。這裡選擇的是淺色背景，藍色強調色。您可以根據需求更改為 `Dark.Red`、`Light.Green` 等。
    *   **自定義應用程式級別樣式：** 在 `ResourceDictionary.MergedDictionaries` 之後，您可以定義自己的 `Style`。這些樣式將應用於所有沒有明確指定 `Style` 的同類型控制項。
        *   **`TargetType="TextBox"`：** 這個樣式會應用於所有 `TextBox` 控制項。
        *   **`{DynamicResource MahApps.Brushes.Accent}`：** 這是 MahApps.Metro 提供的一個動態資源，會根據當前選定的主題強調色自動更新。使用動態資源是實現主題切換的關鍵。

3.  **配置 `StartupUri`：**
    確認 `StartupUri` 指向您的主窗口，例如 `StartupUri="Views/MainWindow.xaml"`。

4.  **`App.xaml.cs` 基礎配置 (未來階段會添加更多內容)：**
    目前 `App.xaml.cs` 保持簡單，但在未來的階段，例如階段 6 錯誤處理，我們會在這裡添加全局異常處理邏輯。

    ```csharp
    // App.xaml.cs
    using System.Windows;

    namespace PythonIpcTool
    {
        /// <summary>
        /// Interaction logic for App.xaml
        /// </summary>
        public partial class App : Application
        {
            // Future: Global exception handling or DI container setup will go here.
        }
    }
    ```

---

完成這兩個步驟後，您的 WPF 專案將具備：
*   明確定義的 C# 和 Python 之間的數據交換格式。
*   一個具有現代外觀的基礎 UI，並已準備好進行進一步的 MVVM 綁定和功能開發。

接下來，我們就可以進入 **階段 1：核心 IPC 服務與資料模型 (Standard I/O)** 的實作，著手編寫實際的 IPC 邏輯了！


使用者點擊 "Execute Script":
ExecutePythonScriptAsync 開始執行。
IsProcessing 設為 true。理論上 ProgressRing 應該會顯示。
_activeCommunicator 被創建並啟動了 Python 進程。
await _activeCommunicator.SendMessageAsync(...) 被呼叫，此時 ExecutePythonScriptAsync 方法會將控制權交還給 UI 執行緒，等待非同步操作完成。
到目前為止一切正常，UI 應該是流暢的。
Python 腳本完成工作:
Python 腳本處理完數據，將結果寫入 stdout，然後正常退出。
Python 進程的退出觸發了 _pythonProcess.Exited 事件。
OnProcessExited 事件被觸發 (災難的開始):
MainViewModel 中的 OnProcessExited 方法在一個背景執行緒上被呼叫。
方法的第一行是 App.Current.Dispatcher.Invoke(() => { ... });。
這行程式碼的意思是：「嘿，UI 執行緒，請暫停你正在做的一切，立即執行我傳遞給你的這段程式碼。」
UI 執行緒上的致命操作:
UI 執行緒接收到請求，開始執行 Invoke 中的程式碼塊。
Log.Information(...) - OK
IsProcessing = false; - OK
StopPythonProcess(); -> CleanUpCommunicator() -> _activeCommunicator.StopProcess() -> _pythonProcess.Kill(true);
Kill() 方法雖然是非阻塞的，但它仍然需要與作業系統進行互動來終結一個進程。在某些情況下，特別是如果該進程正在被作業系統鎖定或處於某種中間狀態，這個呼叫可能會產生短暫的同步延遲。
更嚴重的是，如果 StopProcess 中仍然殘留了任何形式的同步等待（例如 WaitForExit），那麼死鎖就發生了：
背景執行緒上的 Exited 事件正在等待 UI 執行緒完成 Invoke。
UI 執行緒正在 Invoke 內部執行 StopProcess，而 StopProcess 又在同步等待背景進程（也就是觸發 Exited 事件的那個進程）的某些狀態。
兩者互相等待，應用程式完全卡死。 這完美地解釋了為什麼 ProgressRing 的動畫會凍結——因為 UI 執行緒被阻塞了。
為何出現 SendMessageAsync was canceled. 警告:
當 StopPythonProcess() 在 UI 執行緒上被呼叫時，它會 Dispose() _cancellationSource。
這個取消信號會傳播到 ExecutePythonScriptAsync 方法中那個還在 await _activeCommunicator.SendMessageAsync(...) 的「等待點」。
SendMessageAsync 的等待被中斷，拋出一個 OperationCanceledException。
ExecutePythonScriptAsync 的 catch (OperationCanceledException) 區塊被觸發，記錄下 "Execution was canceled..." 的警告。
這看起來就像是使用者手動取消了操作，但實際上是程式碼的清理邏輯「從未來攻擊了過去」，提前取消了一個正在進行中的操作。
為何最終還能收到 success 結果:
因為 Python 腳本在它退出之前，就已經把 success 的結果發送到了 stdout。
StandardIOProcessCommunicator 中的 ReadStreamAsync 背景任務在進程退出之前就已經收到了這個結果，並觸發了 OnOutputReceived 事件。
所以，你看到的 success 結果是真實的，但它是在整個死鎖和混亂的清理流程發生之前就已經收到了。
解決方案
核心思想是：絕對不要在 Dispatcher.Invoke() 這種同步等待的區塊內執行任何可能耗時或與背景執行緒有潛在依賴的操作。 清理工作應該儘可能在背景執行緒完成。

分離 UI 更新和背景清理:
在 OnProcessExited 中，我們現在只將絕對必要且快速的 UI 狀態更新（如 IsProcessing = false）放到 Dispatcher.Invoke() 中。
而可能耗時的清理操作 StopPythonProcess() 則被放到了 Task.Run() 中，確保它在一個背景執行緒上執行，完全不會阻塞 UI 執行緒。
避免不必要的取消警告:
在 ExecutePythonScriptAsync 中，OnProcessExited 可能會在 SendMessageAsync 還在 await 時觸發 StopPythonProcess，進而取消 _cancellationSource。
雖然現在清理是異步的，但為了讓日誌更乾淨，我們可以在 SendMessageAsync 之前添加一個 if (_cancellationSource.IsCancellationRequested) 檢查。但在這個新的非阻塞模型中，這個問題的發生機率大大降低，因為 StopPythonProcess 不再阻塞，整個流程更快。最主要的是，catch (OperationCanceledException) 區塊現在只負責重置 UI 狀態，因為真正的清理會由 OnProcessExited 事件觸發。


問題根源分析 (Precise Diagnosis)
根據我們之前的對話和程式碼演進，這個 30 秒的延遲幾乎可以 100% 肯定是來自於 LocalSocketProcessCommunicator 的 StartProcessAsync 方法中的超時等待。
讓我們來追溯這個致命的執行流程：
使用者點擊 "Execute Script" (假設使用 Local Socket 模式)。
ExecutePythonScriptAsync 被呼叫。
_activeCommunicator 被設置為一個 LocalSocketProcessCommunicator 的新實例。
await _activeCommunicator.StartProcessAsync(...) 被呼叫。
LocalSocketProcessCommunicator.StartProcessAsync 內部:
_listener = new TcpListener(...) 啟動 TCP 伺服器。
_pythonProcess = new Process(...) 創建 Python 進程物件。
_pythonProcess.Start() 啟動 Python。
var acceptTask = _listener.AcceptTcpClientAsync(...) 開始非同步地等待 Python 客戶端的連接。
假設 Python 腳本由於某種原因未能成功連接 (例如，腳本有語法錯誤立即退出，或者防火牆阻擋了本地連接)。
使用者點擊 "X" 關閉視窗:
此時，StartProcessAsync 方法還在 await acceptTask，它並沒有完成，也沒有拋出異常。ExecutePythonScriptAsync 方法也處於暫停狀態。
MainWindow_Closing 事件在 UI 執行緒上被觸發。
viewModel.StopPythonProcessCommand.Execute(null) 被呼叫。
StopPythonProcess() 和 CleanUpCommunicator() 內部:
_activeCommunicator.StopProcess() 被呼叫。
StopProcess 方法會執行 _cancellationTokenSource?.Cancel()。
關鍵點：這個 Cancel() 動作會導致 StartProcessAsync 中的 acceptTask 拋出一個 OperationCanceledException。
異常被 StartProcessAsync 的 catch 區塊捕獲。
LocalSocketProcessCommunicator.StartProcessAsync 的 catch 區塊:
code
C#
catch (Exception ex)
{
    // *** THE DEADLOCK / BLOCKING POINT ***
    StopProcess(); // Re-entrant call to itself!
    throw;
}
catch 區塊再次呼叫了 StopProcess()。這是一個重入 (re-entrant) 呼叫。
此時，StopProcess() 正在 UI 執行緒上執行，而 StartProcessAsync 的 catch 區塊在一個背景執行緒上執行。
這兩個執行緒現在可能會因為爭奪同一個鎖（例如 Process 物件的內部鎖）而產生死鎖 (Deadlock)。或者，StopProcess 中任何殘留的同步等待（即使是很短的 WaitForExit）都會在這裡造成災難，因為它在一個已經被取消的上下文中等待一個可能永遠不會發生的事件。
簡化後的因果鏈： 關閉事件觸發了一個清理操作 (StopProcess)，這個清理操作意外地觸發了一個異常，而這個異常的處理程序又試圖執行同一個（或類似的）清理操作，導致了執行緒之間的死鎖或長時間等待。


為何這樣做：
重入保護 (_isStopping 旗標)：確保 StopProcess 的核心邏輯只會被執行一次，即使它被從多個地方意外呼叫。
打破死鎖鏈: StartProcessAsync 的 catch 區塊不再呼叫 StopProcess，徹底消除了重入死鎖的可能性。ViewModel 的 finally 區塊會負責捕獲異常並呼叫 StopProcess。
快速關閉: MainWindow_Closing 現在直接呼叫 StopPythonProcess。由於我們已經確保了 Communicator 的 StopProcess 方法使用的是非阻塞的 Kill()，所以這個呼叫會立即返回，UI 執行緒不會被阻塞，應用程式會瞬間關閉。







