# hardware_controller.py
import sys, json, socket, time, random

def run_controller(reader, writer):
    # --- Simulate Sensor Reading Thread ---
    # In a real app, this would be in a separate thread.
    import threading
    
    stop_event = threading.Event()

    def sensor_loop():
        while not stop_event.is_set():
            try:
                temp = 25 + random.uniform(-1, 1)
                sensor_data = {"type": "sensor_update", "temperature": round(temp, 2)}
                writer.write(json.dumps(sensor_data) + '\n')
                writer.flush()
                time.sleep(2)
            except:
                break
    
    sensor_thread = threading.Thread(target=sensor_loop)
    sensor_thread.start()

    # --- Main Command Loop ---
    try:
        while True:
            line = reader.readline()
            if not line:
                break
            
            data = json.loads(line)
            command = data.get("command")
            
            if command == "set_led":
                state = data.get("state", "off")
                # In a real app: GPIO.output(LED_PIN, state == "on")
                print(f"[Hardware Sim] LED is now {state}", file=sys.stderr)
                response = {"status": "success", "led_state": state}
                writer.write(json.dumps(response) + '\n')
                writer.flush()
    
    finally:
        stop_event.set()
        sensor_thread.join()

def run_socket_server(port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(('localhost', port))
        s.listen()
        conn, addr = s.accept()
        with conn:
            print(f"Connected by {addr}", file=sys.stderr)
            reader = conn.makefile('r', encoding='utf-8')
            writer = conn.makefile('w', encoding='utf-8')
            run_controller(reader, writer)
            
if __name__ == "__main__":
    # This script runs as a server, so its startup is different.
    # C# should start this script with the port it should listen on.
    if len(sys.argv) == 2:
        run_socket_server(int(sys.argv[1]))
    else:
        print("Usage: python hardware_controller.py <port_to_listen_on>", file=sys.stderr)