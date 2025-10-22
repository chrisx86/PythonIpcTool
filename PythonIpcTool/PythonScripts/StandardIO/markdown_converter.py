# File: PythonScripts/markdown_converter.py
import sys
import json
import markdown

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        markdown_text = input_data.get("markdown_text")

        if markdown_text is None:
            raise ValueError("Missing 'markdown_text' in input JSON.")

        html = markdown.markdown(markdown_text)
        
        response = {"status": "success", "html": html}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()