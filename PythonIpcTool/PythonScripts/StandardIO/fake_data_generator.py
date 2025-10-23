# File: PythonScripts/fake_data_generator.py
import sys
import json
from faker import Faker

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        record_count = input_data.get("record_count", 10)
        schema = input_data.get("schema", {})
        
        fake = Faker()
        fake_data = []

        for _ in range(record_count):
            record = {}
            for key, provider_name in schema.items():
                if hasattr(fake, provider_name):
                    record[key] = getattr(fake, provider_name)()
                else:
                    record[key] = f"Unknown provider: {provider_name}"
            fake_data.append(record)

        response = {"status": "success", "fake_data": fake_data}
        # Using default=str to handle potential date/time objects from Faker
        sys.stdout.write(json.dumps(response, indent=2, default=str) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()