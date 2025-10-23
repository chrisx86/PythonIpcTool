# File: PythonScripts/system_monitor.py
import sys
import json
import psutil

def main():
    try:
        input_line = sys.stdin.readline()
        input_data = json.loads(input_line)
        
        query = input_data.get("query", "all").lower()

        data = {}
        if query == "cpu_usage" or query == "all":
            data["cpu_percent"] = psutil.cpu_percent(interval=0.1)
            data["cpu_count"] = psutil.cpu_count()

        if query == "memory_usage" or query == "all":
            mem = psutil.virtual_memory()
            data["memory_percent"] = mem.percent
            data["memory_total_gb"] = round(mem.total / (1024**3), 2)
            data["memory_available_gb"] = round(mem.available / (1024**3), 2)
        
        if query == "disk_usage" or query == "all":
            disk = psutil.disk_usage('/')
            data["disk_percent"] = disk.percent
            data["disk_total_gb"] = round(disk.total / (1024**3), 2)
            data["disk_free_gb"] = round(disk.free / (1024**3), 2)

        response = {"status": "success", "data": data}
        sys.stdout.write(json.dumps(response) + '\n')

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.exit(1)

if __name__ == "__main__":
    main()