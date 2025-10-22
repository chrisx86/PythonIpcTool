# training_monitor.py
import sys, json, socket, time, random

def start_training(writer, params):
    epochs = params.get("epochs", 10)
    learning_rate = params.get("lr", 0.01)
    
    # Simulate a training loop
    for epoch in range(1, epochs + 1):
        # Simulate some work
        time.sleep(1.5) 
        
        # Simulate changing metrics
        loss = 1.0 / (epoch + random.random())
        accuracy = 1.0 - (1.0 / (epoch * 2 + random.random()))
        
        progress_update = {
            "type": "progress",
            "epoch": epoch,
            "total_epochs": epochs,
            "loss": round(loss, 4),
            "accuracy": round(accuracy, 4)
        }
        writer.write(json.dumps(progress_update) + '\n')
        writer.flush()
        
    final_result = {"type": "result", "message": "Training completed successfully."}
    writer.write(json.dumps(final_result) + '\n')
    writer.flush()

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            # Wait for the start command
            line = reader.readline()
            if not line:
                return

            data = json.loads(line)
            if data.get("command") == "start_training":
                start_training(writer, data.get("params", {}))

    except Exception as e:
        sys.stderr.write(f"Training Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))