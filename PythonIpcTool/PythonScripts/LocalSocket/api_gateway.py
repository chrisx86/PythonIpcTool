# File: PythonScripts/api_gateway.py
import sys, json, socket, requests

def run_socket_mode(port):
    session = requests.Session() # Persistent session object
    
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            while True:
                line = reader.readline()
                if not line: break
                
                try:
                    data = json.loads(line)
                    command = data.get("command")
                    if command == "login":
                        # Simulate login
                        session.headers.update({'Authorization': 'Bearer FAKE_TOKEN'})
                        response = {"status": "success", "message": "Logged in."}
                    elif command == "get_data":
                        # In a real app: resp = session.get(...)
                        response = {"status": "success", "data": {"user": "test", "permissions": ["read"]}}
                    else:
                        raise ValueError("Unknown command")
                except Exception as e:
                    response = {"status": "error", "message": str(e)}

                writer.write(json.dumps(response) + '\n')
                writer.flush()
    except Exception as e:
        sys.stderr.write(f"API Gateway Error: {e}\n")

# ... (main template)