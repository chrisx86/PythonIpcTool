# File: PythonScripts/training_monitor.py
import sys
import json
import socket
import time
import random
import threading

def start_training(writer, params, stop_event):
    epochs = params.get("epochs", 20)
    for epoch in range(1, epochs + 1):
        if stop_event.is_set():
            writer.write(json.dumps({"type": "result", "message": "Training stopped by user."}) + '\n')
            writer.flush()
            return
            
        time.sleep(1.5)  # Simulate training work
        loss = 1.0 / (epoch + random.random())
        accuracy = 1.0 - (1.0 / (epoch * 2 + random.random()))
        
        progress = {"type": "progress", "epoch": epoch, "loss": round(loss, 4), "accuracy": round(accuracy, 4)}
        writer.write(json.dumps(progress) + '\n')
        writer.flush()
        
    final_result = {"type": "result", "message": "Training completed."}
    writer.write(json.dumps(final_result) + '\n')
    writer.flush()

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            stop_event = threading.Event()
            training_thread = None

            while True:
                line = reader.readline()
                if not line:
                    break

                data = json.loads(line)
                command = data.get("command")
                if command == "start_training":
                    training_thread = threading.Thread(target=start_training, args=(writer, data.get("params", {}), stop_event))
                    training_thread.start()
                elif command == "stop_training":
                    stop_event.set()
    except Exception as e:
        sys.stderr.write(f"Training Monitor Error: {e}\n")

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))