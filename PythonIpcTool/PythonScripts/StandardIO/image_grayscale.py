# File: PythonScripts/image_grayscale.py
import sys
import json
from PIL import Image

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        input_path = input_data.get("input_path")
        output_path = input_data.get("output_path")

        if not input_path or not output_path:
            raise ValueError("Missing 'input_path' or 'output_path' in input JSON.")

        with Image.open(input_path) as img:
            grayscale_img = img.convert('L')
            grayscale_img.save(output_path)

        response = {"status": "success", "message": f"Image saved to {output_path}"}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()