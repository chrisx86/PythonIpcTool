好的，我們來詳細說明 **階段 0：專案初始化與基礎架構** 中的兩個關鍵步驟：

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