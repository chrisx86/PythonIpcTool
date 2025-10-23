# File: PythonScripts/formatter.py
import sys
import json
import yaml

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        format_type = input_data.get("format_type", "json").lower()
        content = input_data.get("content", "")

        if format_type == "json":
            parsed_content = json.loads(content)
            formatted_content = json.dumps(parsed_content, indent=4)
        elif format_type == "yaml":
            parsed_content = yaml.safe_load(content)
            formatted_content = yaml.dump(parsed_content, indent=4, sort_keys=False)
        else:
            raise ValueError(f"Unsupported format_type: {format_type}")

        response = {"status": "success", "formatted_content": formatted_content}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()