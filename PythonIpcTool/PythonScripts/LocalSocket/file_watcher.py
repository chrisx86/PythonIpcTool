# File: PythonScripts/file_watcher.py
import sys, json, socket, time
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler

class ChangeHandler(FileSystemEventHandler):
    def __init__(self, writer):
        self.writer = writer

    def on_created(self, event):
        if not event.is_directory:
            notification = {"event": "file_created", "path": event.src_path}
            self.writer.write(json.dumps(notification) + '\n')
            self.writer.flush()
            # Here you could also start a background thread to process the file

def start_watching(writer, path):
    event_handler = ChangeHandler(writer)
    observer = Observer()
    observer.schedule(event_handler, path, recursive=True)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    except (BrokenPipeError, ConnectionResetError):
        observer.stop() # Client disconnected
    observer.join()

# ... (main and run_socket_mode template)