好的，這是一個非常棒的問題！測試 `large_data_script.py` 是**性能測試**和**壓力測試**的核心環節。它驗證的不是「功能是否正確」，而是「應用程式在處理大數據量時是否依然穩定和響應」。

這份指南將包含：
1.  測試的目標。
2.  `large_data_script.py` 的完整程式碼。
3.  詳細的測試步驟。
4.  需要觀察的關鍵指標和預期結果。
5.  可能遇到的問題及排查方法。

---

### **1. 測試目標**

*   **UI 響應性**：驗證在處理大量數據的 IPC 通訊期間，WPF 應用程式的 UI 是否會凍結或卡頓。**這是最重要的測試點。**
*   **IPC 通道健壯性**：確認 Standard I/O 和 Local Socket 兩種模式是否都能夠穩定地傳輸 MB 等級的數據而不會崩潰或丟失數據。
*   **資源使用情況**：觀察應用程式在處理大數據時的 CPU 和記憶體佔用情況，檢查是否存在明顯的記憶體洩漏。
*   **效能比較**：大致比較 Standard I/O 和 Local Socket 在處理大數據時的效能差異。

### **2. `large_data_script.py` 程式碼**

首先，請將以下程式碼保存為 `large_data_script.py`，並放置在您的 `PythonScripts` 資料夾中。

```python
# File: PythonScripts/large_data_script.py
import sys
import json

def main():
    """
    Reads a JSON object from stdin, which should contain a "size" key.
    Generates a list of numbers of that size.
    Writes a new JSON object containing this large list to stdout.
    """
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        
        # Get the desired size of the list from the input JSON, default to 1000.
        size = int(input_data.get("size", 1000))
        
        # Generate a large list of numbers. This is the memory-intensive part.
        large_list = list(range(size))
        
        output_data = {
            "status": "success",
            "message": f"Generated a list with {len(large_list)} items.",
            "data_preview": large_list[:10] # Include a small preview
        }
        
        # This is the performance-critical part: serializing and writing a large JSON.
        # For extremely large data, the final JSON string is constructed first.
        final_json_string = json.dumps(output_data)
        
        sys.stdout.write(final_json_string + '\n')
        sys.stdout.flush()

    except Exception as e:
        error_response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(error_response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

if __name__ == "__main__":
    main()
```
**重要設定**：在 Visual Studio 的方案總管中，右鍵點擊這個檔案，選擇「屬性」，並將「複製到輸出目錄」設為「如果較新則複製」。

### **3. 詳細測試步驟**

#### **準備工作**
1.  **啟動應用程式**：正常啟動您的 Python IPC Tool。
2.  **打開工作管理員**：按下 `Ctrl + Shift + Esc`，切換到「詳細資料」分頁，找到並點擊 `PythonIpcTool.exe`，以便監控其 CPU 和記憶體使用情況。

#### **測試流程**
1.  **配置 Profile 或當前設定**：
    *   在 `Python Interpreter Path` 中，選擇一個有效的 `python.exe`。
    *   在 `Python Script Path` 中，點擊 `Browse...` 並選擇您剛剛創建的 `large_data_script.py`。

2.  **準備輸入數據**：
    在 `Input Data (JSON)` 文字框中，輸入以下內容。我們將從一個大尺寸開始，以充分測試性能。
    ```json
    {
      "size": 1000000
    }
    ```
    *   **說明**：`"size": 1000000` 會讓 Python 生成一個包含一百萬個數字的列表。序列化為 JSON 後，這將是一個大約 **8-9 MB** 的文字字串，足以對 IPC 通道和 UI 渲染構成挑戰。

3.  **執行測試 (Standard I/O 模式)**：
    *   確保 `IPC Communication Mode` 選擇的是 **Standard I/O**。
    *   點擊綠色的 **"Execute Script"** 按鈕。

4.  **在執行期間觀察 (關鍵步驟)**：
    *   **UI 響應性**：**最重要的觀察點是 `ProgressRing`（進度圈）是否在持續平滑地轉動？** 如果它卡住不動，說明 UI 執行緒被阻塞了，這是一個嚴重的性能問題。同時，您應該仍然可以拖動視窗。
    *   **工作管理員**：觀察 `PythonIpcTool.exe` 和新出現的 `python.exe` 進程的 CPU 和記憶體使用率。它們應該會短暫飆高，這是正常的。

5.  **在執行後觀察**：
    *   **UI 狀態**：執行完成後，`ProgressRing` 應消失，"Execute Script" 按鈕應重新變為可用。
    *   **Output 區域**：`Output Data` 文字框中應顯示一個非常巨大的 JSON 字串。**注意：WPF 的 `TextBox` 在渲染數百萬個字元時可能會短暫卡頓，這是 `TextBox` 本身的渲染開銷，通常是可以接受的。**
    *   **Log 區域**：日誌應顯示腳本成功啟動、發送輸入並接收到輸出的訊息。
    *   **工作管理員**：等待幾秒鐘，讓 .NET 的垃圾回收 (GC) 機制運行。`PythonIpcTool.exe` 的記憶體使用量應該會回落到一個接近執行前的水準。如果記憶體持續居高不下，可能存在洩漏。`python.exe` 進程應該已經消失。

6.  **執行測試 (Local Socket 模式)**：
    *   重複步驟 3 到 5，但這次確保 `IPC Communication Mode` 選擇的是 **Local Socket**。
    *   比較兩次執行的總耗時（主觀感覺）以及 UI 的流暢度。

### **4. 預期結果與觀察指標**

| 觀察指標 | 預期通過 (Pass) 的結果 | 預期失敗 (Fail) 的結果 |
| :--- | :--- | :--- |
| **UI 響應性** | `ProgressRing` 持續平滑轉動。視窗可以被拖動。 | `ProgressRing` 卡住不動。視窗無法拖動，UI 凍結。 |
| **執行時間** | 在幾秒鐘內完成（取決於機器性能，例如 2-5 秒）。 | 耗時過長（例如 > 10 秒），或程式直接崩潰。 |
| **記憶體使用** | 執行期間記憶體飆升，但在執行結束後幾秒內回落到接近基線水準。 | 記憶體持續居高不下，或每次執行後基線都顯著升高（記憶體洩漏）。 |
| **CPU 使用** | 執行期間 CPU 飆升至較高水平，執行結束後回落。 | CPU 持續 100% 佔用，或應用程式無響應。 |
| **輸出結果** | `Output Data` 區域成功顯示包含 `data_preview` 的 JSON 字串。 | `Output Data` 區域為空，或 Log 區域顯示 `OutOfMemoryException` 或其他錯誤。 |

### **5. 可能遇到的問題及排查**

*   **問題：UI 完全凍結。**
    *   **原因**：您的 C# 程式碼中很可能在 UI 執行緒上執行了同步的、阻塞性的操作。例如，使用了 `task.Wait()` 或 `task.Result`，或者在讀寫流時沒有使用 `...Async` 的非同步版本。
    *   **解決方案**：審查從按鈕點擊事件到 `Communicator` 服務的整個呼叫鏈，確保每一步都使用了 `async`/`await`。

*   **問題：應用程式拋出 `OutOfMemoryException`。**
    *   **原因**：您嘗試生成的數據量對於您機器的可用記憶體來說太大了。一個 8 MB 的字串需要被 C# 和 Python 同時載入到記憶體中，這會佔用比字串本身更多的空間。
    *   **解決方案**：這標示了您應用程式的性能極限。在 `Input Data` 中嘗試一個較小的 `size`，例如 `500000`。對於需要處理超大數據（GB 等級）的場景，需要重新設計 IPC 協議，採用分塊 (chunking) 或串流的方式，而不是一次性傳輸整個 JSON。

*   **問題：Python 腳本在 Log 中報錯。**
    *   **原因**：可能是 Python 本身記憶體不足，或者 `json.dumps()` 在處理極大列表時失敗。
    *   **解決方案**：同樣，減小 `size` 的值進行測試。

通過執行這些性能測試，您不僅能驗證應用程式的穩定性，還能深入了解 `async/await` 在保持 UI 響應性方面的重要性，這對於開發高品質的桌面應用至關重要。