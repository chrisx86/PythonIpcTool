# File: PythonScripts/repl_engine.py
import sys
import json
import socket
from io import StringIO

def run_socket_mode(port):
    local_namespace = {}  # Persistent namespace to store variables
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            # Send a ready signal
            writer.write(json.dumps({"status": "ready"}) + '\n')
            writer.flush()
            
            while True:
                line = reader.readline()
                if not line:
                    break  # Connection closed

                try:
                    data = json.loads(line)
                    if data.get("command") == "execute":
                        code_to_run = data.get("code", "")
                        
                        # Capture stdout (e.g., from print statements)
                        old_stdout = sys.stdout
                        redirected_output = sys.stdout = StringIO()
                        
                        exec(code_to_run, globals(), local_namespace)
                        
                        sys.stdout = old_stdout  # Restore stdout
                        output_str = redirected_output.getvalue()
                        
                        response = {"status": "success", "output": output_str}
                except Exception as e:
                    response = {"status": "error", "message": f"{type(e).__name__}: {e}"}
                
                writer.write(json.dumps(response) + '\n')
                writer.flush()
    except Exception as e:
        sys.stderr.write(f"REPL Engine Fatal Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))
    else:
        sys.stderr.write("This script must be run in socket mode.\n")