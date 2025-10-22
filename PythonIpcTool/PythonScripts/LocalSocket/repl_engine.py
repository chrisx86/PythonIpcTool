# repl_engine.py
import sys, json, socket

def run_socket_mode(port):
    local_namespace = {} # Persistent namespace for variables
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            while True:
                line = reader.readline()
                if not line:
                    break
                
                try:
                    data = json.loads(line)
                    if data.get("command") == "execute":
                        code = data.get("code", "")
                        # Redirect stdout to capture print() statements
                        from io import StringIO
                        old_stdout = sys.stdout
                        redirected_output = sys.stdout = StringIO()
                        
                        exec(code, local_namespace)
                        
                        sys.stdout = old_stdout # Restore stdout
                        output = redirected_output.getvalue()
                        response = {"status": "success", "output": output}
                except Exception as e:
                    response = {"status": "error", "message": str(e)}
                
                writer.write(json.dumps(response) + '\n')
                writer.flush()
    except Exception as e:
        # This will be caught by the C# app's stderr listener
        sys.stderr.write(f"REPL Engine Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))
    else:
        sys.stderr.write("This script only supports socket mode.\n")