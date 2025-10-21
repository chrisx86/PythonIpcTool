# File: PythonIpcTool/PythonScripts/simple_processor.py
import sys
import json
import socket
import time

# --- NEW: Socket Mode Logic ---
def run_socket_mode(port):
    """Connects to a C# server on a given port and processes messages in a loop."""
    try:
        # Create a TCP/IP socket client
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
            client_socket.connect(('localhost', port))
            
            # Wrap the socket in a file-like object for easier line-by-line reading
            # This handles message framing (waiting for newline) elegantly
            reader = client_socket.makefile('r', encoding='utf-8')
            writer = client_socket.makefile('w', encoding='utf-8')

            # Continuously process messages from the server
            while True:
                line = reader.readline()
                if not line:
                    # Connection closed by the server
                    break
                
                # Process the received line (which is a JSON string)
                process_data(line, writer)

    except ConnectionRefusedError:
        sys.stderr.write(json.dumps({"error": f"Connection refused on port {port}. Is the server running?"}) + '\n')
        sys.stderr.flush()
        sys.exit(1)
    except Exception as e:
        sys.stderr.write(json.dumps({"error": f"Socket communication error: {str(e)}"}) + '\n')
        sys.stderr.flush()
        sys.exit(1)

# --- Standard I/O Mode Logic (refactored into a function) ---
def run_stdio_mode():
    """Reads from stdin and writes to stdout/stderr."""
    # This loop allows the script to process multiple inputs if C# sends them sequentially
    for line in sys.stdin:
        if not line.strip():
            continue
        process_data(line, sys.stdout)

# --- Shared Processing Logic ---
def process_data(json_line, output_stream):
    """Parses a JSON line, processes it, and writes the result to the given output stream."""
    try:
        input_data = json.loads(json_line)
        value = input_data.get('value', 'default')
        numbers = input_data.get('numbers', [])

        # Simulate some processing
        result_message = f"Processed in Python: '{value}'"
        
        output_data = {"result": result_message, "status": "success"}
        if numbers:
            output_data["sum"] = sum(numbers)
        
        # Write the JSON response followed by a newline to the output stream
        output_stream.write(json.dumps(output_data) + '\n')
        output_stream.flush() # Ensure data is sent immediately

    except json.JSONDecodeError:
        sys.stderr.write(json.dumps({"error": "Invalid JSON input received."}) + '\n')
        sys.stderr.flush()
    except Exception as e:
        sys.stderr.write(json.dumps({"error": f"Python script processing error: {str(e)}"}) + '\n')
        sys.stderr.flush()

# --- Main entry point ---
if __name__ == "__main__":
    # Check command-line arguments to determine the mode
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        try:
            port_num = int(sys.argv[2])
            run_socket_mode(port_num)
        except ValueError:
            sys.stderr.write(json.dumps({"error": "Invalid port number provided."}) + '\n')
            sys.stderr.flush()
            sys.exit(1)
    else:
        # Default to Standard I/O mode if no specific arguments are given
        run_stdio_mode()