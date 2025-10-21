# simple_processor.py
import sys
import json

def main():
    try:
        # Read input from stdin
        line = sys.stdin.readline()
        if not line:
            sys.exit(0) # Exit if no more input

        input_data = json.loads(line)
        
        # Perform some processing
        processed_value = f"Processed: {input_data.get('value', 'No value provided')}"
        
        # Write output to stdout
        output_data = {"result": processed_value, "status": "success"}
        sys.stdout.write(json.dumps(output_data) + '\n')
        sys.stdout.flush()

    except json.JSONDecodeError:
        sys.stderr.write(json.dumps({"error": "Invalid JSON input."}) + '\n')
        sys.stderr.flush()
    except Exception as e:
        sys.stderr.write(json.dumps({"error": f"Python script error: {str(e)}"}) + '\n')
        sys.stderr.flush()

if __name__ == "__main__":
    main()
    

"""
模式切換： 腳本現在會檢查 sys.argv。如果 C# 啟動它時帶有 socket 和端口號參數 (例如 python simple_processor.py socket 12345)，它就會進入 Socket 模式。否則，它會像以前一樣運行在 Standard I/O 模式。
Socket 客戶端： 在 run_socket_mode 中，Python 作為客戶端連接到 localhost 和 C# 提供的端口。
持續通訊： 使用 while True 循環，腳本可以保持運行並持續接收和處理來自 C# 的多條訊息，直到 Socket 連接被 C# 伺服器關閉。
訊息邊界 (Framing)： 使用 makefile('r') 和 readline() 簡化了 Socket 數據流的處理。它會一直讀取直到遇到換行符 \n，這完美地解決了 TCP/IP 流式傳輸中訊息邊界的問題。我們只需要確保 C# 在發送時也在每條 JSON 訊息後附加一個 \n。
"""
