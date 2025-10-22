# stock_ticker.py
import sys, json, socket, time, random

def stream_stock_data(writer, symbol):
    # In a real scenario, you'd use a library like websocket-client here.
    # We will simulate receiving data every second.
    price = 150.0
    while True:
        try:
            time.sleep(1)
            # Simulate price fluctuation
            price += random.uniform(-0.5, 0.5)
            # Simulate RSI calculation
            rsi = 50 + random.uniform(-10, 10)
            
            tick_data = {
                "type": "tick",
                "symbol": symbol,
                "price": round(price, 2),
                "rsi": round(rsi, 2)
            }
            writer.write(json.dumps(tick_data) + '\n')
            writer.flush()
        except (BrokenPipeError, ConnectionResetError):
            # C# client has disconnected, so we exit gracefully
            print("Client disconnected. Exiting.", file=sys.stderr)
            break
        except Exception as e:
            sys.stderr.write(f"Streaming Error: {e}\n")
            sys.stderr.flush()
            break

def run_socket_mode(port):
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.connect(('localhost', port))
            reader = s.makefile('r', encoding='utf-8')
            writer = s.makefile('w', encoding='utf-8')
            
            line = reader.readline()
            if not line:
                return

            data = json.loads(line)
            if data.get("command") == "subscribe":
                stream_stock_data(writer, data.get("symbol", "SIM_STOCK"))
                
    except Exception as e:
        sys.stderr.write(f"Stock Ticker Error: {e}\n")
        sys.stderr.flush()

if __name__ == "__main__":
    if len(sys.argv) == 3 and sys.argv[1] == 'socket':
        run_socket_mode(int(sys.argv[2]))