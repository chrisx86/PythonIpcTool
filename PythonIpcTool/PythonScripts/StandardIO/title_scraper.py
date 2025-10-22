# File: PythonScripts/title_scraper.py
import sys
import json
import requests
from bs4 import BeautifulSoup

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        url = input_data.get("url")

        if not url:
            raise ValueError("Missing 'url' in input JSON.")

        headers = {'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36'}
        http_response = requests.get(url, headers=headers, timeout=10)
        http_response.raise_for_status()  # Raise an exception for bad status codes (4xx or 5xx)

        soup = BeautifulSoup(http_response.text, 'html.parser')
        
        title = "No title found"
        if soup.title and soup.title.string:
            title = soup.title.string.strip()

        response = {"status": "success", "title": title}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()