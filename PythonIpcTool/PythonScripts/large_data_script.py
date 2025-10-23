import sys
import json

def main():
    """
    Reads a JSON object from stdin, which should contain a "size" key.
    Generates a list of numbers of that size.
    Writes a new JSON object containing this large list to stdout.
    """
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        
        # Get the desired size of the list from the input JSON, default to 1000.
        size = int(input_data.get("size", 1000))
        
        # Generate a large list of numbers. This is the memory-intensive part.
        large_list = list(range(size))
        
        output_data = {
            "status": "success",
            "message": f"Generated a list with {len(large_list)} items.",
            "data_preview": large_list[:10] # Include a small preview
        }
        
        # This is the performance-critical part: serializing and writing a large JSON.
        # For extremely large data, the final JSON string is constructed first.
        final_json_string = json.dumps(output_data)
        
        sys.stdout.write(final_json_string + '\n')
        sys.stdout.flush()

    except Exception as e:
        error_response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(error_response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

if __name__ == "__main__":
    main()