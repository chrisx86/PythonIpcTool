# File: PythonScripts/csv_analyzer.py
import sys
import json
import pandas as pd

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        file_path = input_data.get("file_path")
        column_name = input_data.get("column_name")

        if not file_path:
            raise ValueError("Missing 'file_path' in input JSON.")

        df = pd.read_csv(file_path)

        if column_name and column_name in df.columns:
            stats_series = df[column_name].describe()
            stats_dict = stats_series.to_dict()
        else:
            stats_df = df.describe()
            stats_dict = stats_df.to_dict()

        response = {"status": "success", "stats": stats_dict}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()