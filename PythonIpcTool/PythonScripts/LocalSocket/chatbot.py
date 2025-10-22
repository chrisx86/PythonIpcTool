# chatbot.py
import sys, json, socket
from textblob import TextBlob

def process_data_line(json_line, writer):
    try:
        data = json.loads(json_line)
        query = data.get("query", "")
        
        # Simple rule-based chatbot + sentiment analysis
        if "weather" in query.lower():
            response_text = "It's always sunny in the world of code!"
        elif "name" in query.lower():
            response_text = "You can call me PyBot."
        else:
            # Use TextBlob for a generic sentiment response
            sentiment = TextBlob(query).sentiment.polarity
            if sentiment > 0.5:
                response_text = "That's great to hear!"
            elif sentiment < -0.5:
                response_text = "I'm sorry to hear that."
            else:
                response_text = "Interesting. Tell me more."
                
        response = {"response": response_text}
        writer.write(json.dumps(response) + '\n')
        writer.flush()

    except Exception as e:
        sys.stderr.write(f"Chatbot Error: {e}\n")

# --- Standard Socket/Stdio template from here ---
def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            while True:
                line = reader.readline()
                if not line: break
                process_data_line(line, writer)
    except Exception as e:
        sys.stderr.write(f"Socket Error: {e}\n")

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))