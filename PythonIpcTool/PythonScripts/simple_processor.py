# simple_processor.py
import sys
import json

def main():
    try:
        # Read input from stdin
        line = sys.stdin.readline()
        if not line:
            sys.exit(0) # Exit if no more input

        input_data = json.loads(line)
        
        # Perform some processing
        processed_value = f"Processed: {input_data.get('value', 'No value provided')}"
        
        # Write output to stdout
        output_data = {"result": processed_value, "status": "success"}
        sys.stdout.write(json.dumps(output_data) + '\n')
        sys.stdout.flush()

    except json.JSONDecodeError:
        sys.stderr.write(json.dumps({"error": "Invalid JSON input."}) + '\n')
        sys.stderr.flush()
    except Exception as e:
        sys.stderr.write(json.dumps({"error": f"Python script error: {str(e)}"}) + '\n')
        sys.stderr.flush()

if __name__ == "__main__":
    main()