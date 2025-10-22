# common_template.py
import sys
import json
import socket

def process_data_line(json_line, output_stream):
    # Specific logic for each script will go here
    pass

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            while True:
                line = reader.readline()
                if not line:
                    break
                process_data_line(line, writer)
    except Exception as e:
        sys.stderr.write(f"Socket Error: {e}\n")
        sys.stderr.flush()

def run_stdio_mode():
    for line in sys.stdin:
        if not line.strip():
            continue
        process_data_line(line, sys.stdout)

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))
    else:
        run_stdio_mode()