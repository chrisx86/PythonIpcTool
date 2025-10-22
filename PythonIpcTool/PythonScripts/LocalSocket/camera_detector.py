# camera_detector.py
import sys, json, socket, time, random
# import cv2 # Uncomment for real use

def detect_from_camera(writer):
    # In a real scenario:
    # cap = cv2.VideoCapture(0)
    # yolo_model = ... load model ...
    
    # We will simulate detection
    labels = ["person", "car", "dog", "laptop"]
    
    while True:
        try:
            time.sleep(0.5) # Simulate frame rate
            
            # Simulate object detection on a frame
            # if not ret: break # In real code
            # results = yolo_model.predict(frame)
            
            # Create simulated results
            detected_objects = []
            if random.random() > 0.3: # 70% chance to detect something
                obj_count = random.randint(1, 2)
                for _ in range(obj_count):
                    detected_objects.append({
                        "label": random.choice(labels),
                        "box": [random.randint(0, 600), random.randint(0, 400), 
                                random.randint(20, 100), random.randint(20, 100)]
                    })
            
            if detected_objects:
                response = {"type": "detection", "objects": detected_objects}
                writer.write(json.dumps(response) + '\n')
                writer.flush()
                
        except (BrokenPipeError, ConnectionResetError):
            print("Client disconnected. Exiting.", file=sys.stderr)
            break
        except Exception as e:
            sys.stderr.write(f"Detection Error: {e}\n")
            sys.stderr.flush()
            break

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            writer = s.makefile('w', encoding='utf-8')
            # No input needed, just start detecting
            detect_from_camera(writer)
    except Exception as e:
        sys.stderr.write(f"Camera Detector Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))