# large_data_script.py
import sys
import json

# Read a single line of input
line = sys.stdin.readline()
input_data = json.loads(line)

# Generate a large JSON payload as output
large_list = list(range(input_data.get("size", 1000)))
output_data = {
    "status": "success",
    "message": f"Generated a list of {len(large_list)} items.",
    "data": large_list
}

# Write the large JSON back to stdout
sys.stdout.write(json.dumps(output_data) + '\n')
sys.stdout.flush()