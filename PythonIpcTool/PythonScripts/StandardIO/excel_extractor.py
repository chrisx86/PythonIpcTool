# File: PythonScripts/excel_extractor.py
import sys
import json
import pandas as pd

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        file_path = input_data.get("file_path")
        sheet_name = input_data.get("sheet_name", 0) # Default to the first sheet

        if not file_path:
            raise ValueError("Missing 'file_path' in input.")

        df = pd.read_excel(file_path, sheet_name=sheet_name)
        # Convert dataframe to a list of dictionaries
        data_records = df.to_dict(orient='records')

        response = {"status": "success", "data": data_records}
        sys.stdout.write(json.dumps(response, indent=2) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()