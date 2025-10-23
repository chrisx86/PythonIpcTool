# File: PythonScripts/log_analyzer.py
import sys
import json
import socket
import time
import re

def watch_log_file(writer, file_path):
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            f.seek(0, 2)  # Go to the end of the file
            while True:
                line = f.readline()
                if not line:
                    time.sleep(0.1)  # Wait for new lines
                    continue
                
                # Simple parsing logic
                level = "INFO"
                if "ERROR" in line.upper():
                    level = "ERROR"
                elif "WARN" in line.upper():
                    level = "WARN"
                
                log_entry = {"type": "log_entry", "level": level, "message": line.strip()}
                writer.write(json.dumps(log_entry) + '\n')
                writer.flush()
    except (BrokenPipeError, ConnectionResetError):
        pass # Client disconnected
    except Exception as e:
        error_msg = {"type": "error", "message": str(e)}
        writer.write(json.dumps(error_msg) + '\n')
        writer.flush()

def run_socket_mode(port):
    # Standard connection logic
    # ...
    # Inside the loop, on receiving "watch_log" command:
    # watch_log_file(writer, data.get("path"))
    pass # Implementation is similar to training_monitor.py