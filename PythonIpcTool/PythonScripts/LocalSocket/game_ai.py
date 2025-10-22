# game_ai.py
import sys, json, socket

def process_data_line(json_line, writer):
    try:
        data = json.loads(json_line)
        if data.get("event") == "update":
            game_state = data.get("game_state", {})
            player_pos = game_state.get("player_pos", {"x": 0, "y": 0})
            ai_pos = game_state.get("ai_pos", {"x": 0, "y": 0})
            
            # Simple AI: move towards the player
            action = {"action": "move_towards", "target": player_pos}
            writer.write(json.dumps(action) + '\n')
            writer.flush()

    except Exception as e:
        sys.stderr.write(f"Game AI Error: {e}\n")

# --- Standard Socket/Stdio template ---
def run_socket_mode(port):
    # ...
    pass
# ...