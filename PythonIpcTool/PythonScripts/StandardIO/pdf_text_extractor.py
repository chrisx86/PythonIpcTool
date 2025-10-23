# File: PythonScripts/pdf_text_extractor.py
import sys
import json
from PyPDF2 import PdfReader

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        file_path = input_data.get("file_path")
        if not file_path:
            raise ValueError("Missing 'file_path'.")
            
        reader = PdfReader(file_path)
        full_text = []
        for page in reader.pages:
            full_text.append(page.extract_text())
        
        text_content = "\n\n".join(full_text)

        response = {"status": "success", "page_count": len(reader.pages), "text_content": text_content}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()