# File: PythonScripts/sentiment_analyzer.py
import sys
import json
from textblob import TextBlob

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        text = input_data.get("text")

        if text is None:
            raise ValueError("Missing 'text' in input JSON.")

        blob = TextBlob(text)
        sentiment = blob.sentiment
        
        classification = "Neutral"
        if sentiment.polarity > 0.1:
            classification = "Positive"
        elif sentiment.polarity < -0.1:
            classification = "Negative"

        response = {
            "status": "success",
            "sentiment": {
                "polarity": sentiment.polarity,
                "subjectivity": sentiment.subjectivity,
                "classification": classification
            }
        }

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()