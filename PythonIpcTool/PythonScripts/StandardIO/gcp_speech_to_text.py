# File: PythonScripts/gcp_speech_to_text.py
import sys
import json
from google.cloud import speech

def main():
    try:
        input_line = sys.stdin.readline()
        if not input_line:
            sys.exit(0)

        input_data = json.loads(input_line)
        audio_file_path = input_data.get("file_to_analyze")

        if not audio_file_path:
            raise ValueError("Missing 'file_to_analyze' in input JSON.")

        client = speech.SpeechClient()

        with open(audio_file_path, "rb") as audio_file:
            content = audio_file.read()

        audio = speech.RecognitionAudio(content=content)
        config = speech.RecognitionConfig(
            encoding=speech.RecognitionConfig.AudioEncoding.LINEAR16, # Example, adjust as needed
            sample_rate_hertz=16000,                                  # Example, adjust as needed
            language_code="en-US",
        )
        
        gcp_response = client.recognize(config=config, audio=audio)
        
        transcripts = [result.alternatives[0].transcript for result in gcp_response.results]
        full_transcript = " ".join(transcripts)

        response = {"status": "success", "transcript": full_transcript}

    except Exception as e:
        response = {"status": "error", "message": str(e)}
        sys.stderr.write(json.dumps(response) + '\n')
        sys.stderr.flush()
        sys.exit(1)

    sys.stdout.write(json.dumps(response) + '\n')
    sys.stdout.flush()

if __name__ == "__main__":
    main()