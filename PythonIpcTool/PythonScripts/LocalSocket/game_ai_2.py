# File: PythonScripts/game_ai.py
import sys, json, socket

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            while True:
                line = reader.readline()
                if not line: break
                
                try:
                    data = json.loads(line)
                    if data.get("event") == "game_tick":
                        state = data.get("state", {})
                        player_pos = state.get("player_pos", [0,0])
                        ai_pos = state.get("ai_pos", [0,0])
                        
                        # Simple AI: move towards the player
                        dx = player_pos[0] - ai_pos[0]
                        dy = player_pos[1] - ai_pos[1]
                        
                        # Normalize direction (simplified)
                        length = max(1, (dx**2 + dy**2)**0.5)
                        
                        action = {"action": "move_by", "delta": [dx/length, dy/length]}
                        writer.write(json.dumps(action) + '\n')
                        writer.flush()
                except Exception as e:
                    # In a game, we might just log to stderr and continue
                    sys.stderr.write(f"AI Tick Error: {e}\n")

    except Exception as e:
        sys.stderr.write(f"Game AI Connection Error: {e}\n")

# ... (main template)