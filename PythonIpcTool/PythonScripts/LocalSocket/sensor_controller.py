# File: PythonScripts/sensor_controller.py
import sys, json, socket, time, random, threading

# Mock GPIO
# class GPIO:
#     OUT = 1
#     def setup(pin, mode): print(f"GPIO: Pin {pin} set to mode {mode}")
#     def output(pin, state): print(f"GPIO: Pin {pin} set to state {state}")

def sensor_reader(writer, stop_event):
    while not stop_event.is_set():
        try:
            temp = 30 + random.uniform(-0.5, 0.5)
            pressure = 1013 + random.uniform(-2, 2)
            sensor_data = {"type": "sensor_data", "temperature": round(temp, 1), "pressure": round(pressure)}
            writer.write(json.dumps(sensor_data) + '\n')
            writer.flush()
            time.sleep(1)
        except:
            break

def run_server(port):
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.bind(('0.0.0.0', port)) # Listen on all network interfaces
        s.listen()
        print(f"Sensor controller listening on port {port}...", file=sys.stderr)
        conn, addr = s.accept()
        with conn:
            print(f"Controller connected by {addr}", file=sys.stderr)
            reader = conn.makefile('r', encoding='utf-8')
            writer = conn.makefile('w', encoding='utf-8')
            stop_event = threading.Event()
            
            sensor_thread = threading.Thread(target=sensor_reader, args=(writer, stop_event))
            sensor_thread.start()
            
            try:
                while True:
                    line = reader.readline()
                    if not line: break
                    data = json.loads(line)
                    if data.get("command") == "set_valve":
                        print(f"Received command: {data}", file=sys.stderr)
                        # GPIO.output(...)
            finally:
                stop_event.set()
                sensor_thread.join()

if __name__ == "__main__":
    if len(sys.argv) == 2:
        run_server(int(sys.argv[1]))