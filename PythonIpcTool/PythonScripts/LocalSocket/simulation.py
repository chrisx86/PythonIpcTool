# simulation.py
import sys, json, socket, time

def run_simulation(reader, writer):
    # Simulation parameters with defaults
    params = {"damping": 0.1, "gravity": 9.8}
    position = 100.0
    velocity = 0.0
    
    # --- Main Simulation Loop ---
    last_time = time.time()
    while True:
        try:
            # --- Non-blocking read for parameter updates ---
            # Set a timeout so we don't block forever waiting for C#
            reader_socket = reader.fileno()
            s = socket.fromfd(reader_socket, socket.AF_INET, socket.SOCK_STREAM)
            s.settimeout(0.01) # 10ms timeout
            try:
                line = reader.readline()
                if not line: break
                
                update_data = json.loads(line)
                if update_data.get("command") == "set_param":
                    key, value = update_data.get("key"), update_data.get("value")
                    if key in params:
                        params[key] = float(value)
                        print(f"Updated {key} to {value}", file=sys.stderr)
            except (socket.timeout, json.JSONDecodeError):
                pass # No new data, just continue simulation

            # --- Physics calculation ---
            current_time = time.time()
            dt = current_time - last_time
            last_time = current_time
            
            force = -params["gravity"] - (velocity * params["damping"])
            velocity += force * dt
            position += velocity * dt
            
            if position < 0:
                position = 0
                velocity *= -0.8 # Bounce

            # --- Push state to C# ---
            state_update = {"type": "sim_state", "position": round(position, 2), "velocity": round(velocity, 2)}
            writer.write(json.dumps(state_update) + '\n')
            writer.flush()
            
            time.sleep(1/60) # 60 FPS
            
        except (BrokenPipeError, ConnectionResetError):
            break

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            run_simulation(reader, writer)
    except Exception as e:
        sys.stderr.write(f"Simulation Error: {e}\n")

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))