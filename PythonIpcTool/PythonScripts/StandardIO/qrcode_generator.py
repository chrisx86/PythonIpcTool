# File: PythonScripts/qrcode_generator.py
import sys
import json
import qrcode

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        data = input_data.get("data")
        output_path = input_data.get("output_path")

        if not data or not output_path:
            raise ValueError("Missing 'data' or 'output_path' in input JSON.")

        img = qrcode.make(data)
        img.save(output_path)
        
        response = {"status": "success", "file_path": output_path}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()