# File: PythonScripts/audio_processor.py
import sys, json, socket, sounddevice, numpy

def audio_callback(indata, frames, time, status, writer):
    if status:
        sys.stderr.write(str(status) + '\n')
    volume_norm = numpy.linalg.norm(indata) * 10
    update = {"type": "volume_update", "rms": float(volume_norm)}
    try:
        writer.write(json.dumps(update) + '\n')
        writer.flush()
    except:
        # Stop stream on error
        raise sounddevice.CallbackStop

def start_mic_stream(writer):
    with sounddevice.InputStream(callback=lambda i,f,t,s: audio_callback(i,f,t,s,writer)):
        print("Microphone stream started. Press Ctrl+C to stop.", file=sys.stderr)
        # Keep the script alive while the callback runs in the background
        while True:
            socket.socket().recv(1) # Hack to wait for client disconnect

# ... (main template, on "start_mic" command call start_mic_stream)