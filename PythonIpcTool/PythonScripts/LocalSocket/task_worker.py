# task_worker.py
import sys, json, socket, time, random

def process_data_line(json_line, writer):
    try:
        data = json.loads(json_line)
        if data.get("command") == "calculate_chunk":
            chunk_data = data.get("data", [])
            
            # Simulate a CPU-intensive task
            time.sleep(random.uniform(1, 3))
            
            result = sum(chunk_data)
            response = {"result": result, "chunk_size": len(chunk_data)}
            writer.write(json.dumps(response) + '\n')
            writer.flush()
    except Exception as e:
        sys.stderr.write(f"Worker Error: {e}\n")
        
# --- Standard Socket/Stdio template ---
# This script is a worker, so it connects to the C# master.
# The C# app would need to be a socket SERVER in this case.
# The template is the same as the chatbot's.
def run_socket_mode(port):
    # ...
    pass
# ...