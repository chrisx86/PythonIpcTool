# File: PythonScripts/hash_calculator.py
import sys
import json
import hashlib

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        file_path = input_data.get("file_path")
        algorithm = input_data.get("algorithm", "sha256").lower()
        
        if not file_path:
            raise ValueError("Missing 'file_path'.")
            
        hasher = hashlib.new(algorithm)
        
        with open(file_path, 'rb') as f:
            # Read in chunks to handle large files efficiently
            while chunk := f.read(8192):
                hasher.update(chunk)
        
        file_hash = hasher.hexdigest()

        response = {"status": "success", "algorithm": algorithm, "hash": file_hash}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()