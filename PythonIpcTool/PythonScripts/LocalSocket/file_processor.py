# file_processor.py
import sys, json, socket, time

def process_large_file(writer, file_path):
    try:
        # In a real app, you would open the file_path.
        # Here we simulate a large file with 100 lines.
        total_lines = 100
        for i in range(total_lines):
            time.sleep(0.1) # Simulate work on each line
            
            # Send progress update every 10 lines
            if (i + 1) % 10 == 0:
                percent = ((i + 1) / total_lines) * 100
                progress_update = {"type": "progress", "percent": round(percent, 2)}
                writer.write(json.dumps(progress_update) + '\n')
                writer.flush()
        
        final_result = {"type": "result", "summary": {"lines_processed": total_lines}}
        writer.write(json.dumps(final_result) + '\n')
        writer.flush()

    except Exception as e:
        error_result = {"type": "error", "message": str(e)}
        writer.write(json.dumps(error_result) + '\n')
        writer.flush()

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            line = reader.readline()
            if not line: return

            data = json.loads(line)
            if data.get("command") == "process_file":
                process_large_file(writer, data.get("path"))
                
    except Exception as e:
        sys.stderr.write(f"File Processor Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))