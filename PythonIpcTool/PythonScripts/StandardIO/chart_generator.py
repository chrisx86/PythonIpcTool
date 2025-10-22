# File: PythonScripts/chart_generator.py
import sys
import json
import matplotlib.pyplot as plt

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        
        plt.figure()
        plt.plot(input_data.get("x_data", []), input_data.get("y_data", []))
        plt.title(input_data.get("title", "Chart"))
        plt.xlabel(input_data.get("xlabel", "X-axis"))
        plt.ylabel(input_data.get("ylabel", "Y-axis"))
        plt.grid(True)
        
        output_path = input_data.get("output_path")
        if not output_path:
            raise ValueError("Missing 'output_path' in input JSON.")
            
        plt.savefig(output_path)
        plt.close()

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