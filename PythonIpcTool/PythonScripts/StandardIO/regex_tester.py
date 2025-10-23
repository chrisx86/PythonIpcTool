# File: PythonScripts/regex_tester.py
import sys
import json
import re

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        text = input_data.get("text", "")
        pattern = input_data.get("pattern", "")
        operation = input_data.get("operation", "findall").lower()

        result = None
        if operation == "findall":
            result = re.findall(pattern, text)
        elif operation == "search":
            match = re.search(pattern, text)
            if match:
                result = {"span": match.span(), "group": match.group(0)}
        elif operation == "sub":
            replace_with = input_data.get("replace_with", "")
            result = re.sub(pattern, replace_with, text)
        else:
            raise ValueError(f"Unsupported operation: {operation}")

        response = {"status": "success", "matches": result}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()