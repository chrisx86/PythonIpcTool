# File: PythonScripts/yt_downloader.py
import sys
import json
import subprocess
import os

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        video_url = input_data.get("video_url")
        download_path = input_data.get("download_path", ".")

        if not video_url:
            raise ValueError("Missing 'video_url' in input JSON.")

        # Ensure download path exists
        os.makedirs(download_path, exist_ok=True)
        
        # Construct command: yt-dlp -o "path/to/download/%(title)s.%(ext)s" <URL>
        # The output template ensures the filename is the video title.
        output_template = os.path.join(download_path, '%(title)s.%(ext)s')
        command = ['yt-dlp', '-o', output_template, video_url]
        
        # Execute the command
        result = subprocess.run(command, check=True, capture_output=True, text=True, encoding='utf-8')

        response = {
            "status": "success",
            "message": "Video downloaded successfully.",
            "yt-dlp-output": result.stdout
        }

    except subprocess.CalledProcessError as e:
        # This catches errors from yt-dlp itself (e.g., video not found)
        response = {"status": "error", "message": "yt-dlp failed.", "details": e.stderr}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)
    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()