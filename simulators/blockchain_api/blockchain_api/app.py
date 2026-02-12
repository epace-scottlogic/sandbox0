import json
import threading
from flask import Flask
from flask_sock import Sock

from blockchain_api.websocket_handler import WebSocketHandler
from blockchain_api.trade_generator import TradeGenerator
from blockchain_api.interval_scheduler import IntervalScheduler


app = Flask(__name__)
sock = Sock(app)


@sock.route("/ws")
def websocket(ws):
    handler = WebSocketHandler()
    trade_generators: dict[str, TradeGenerator] = {}
    schedulers: dict[str, IntervalScheduler] = {}
    lock = threading.Lock()

    def send_trade(symbol: str):
        if symbol in trade_generators and handler.is_subscribed(symbol):
            trade = trade_generators[symbol].generate_trade()
            update = handler.format_trade_update(trade)
            try:
                ws.send(update)
                print(f"sent: {TradeGenerator.format_trade(trade)}")
            except Exception:
                pass

    try:
        while True:
            message = ws.receive()
            if message is None:
                break

            print(f"received: {message}")
            response = handler.handle_message(message)
            ws.send(response)
            print(f"sent: {response}")

            response_data = json.loads(response)
            if response_data.get("event") == "subscribed":
                symbol = response_data.get("symbol")
                if symbol and symbol not in trade_generators:
                    with lock:
                        trade_generators[symbol] = TradeGenerator(symbol)
                        scheduler = IntervalScheduler(min_interval=0.5, max_interval=3.0)
                        schedulers[symbol] = scheduler
                        scheduler.start(lambda s=symbol: send_trade(s))

            elif response_data.get("event") == "unsubscribed":
                symbol = response_data.get("symbol")
                if symbol and symbol in schedulers:
                    with lock:
                        schedulers[symbol].stop()
                        del schedulers[symbol]
                        if symbol in trade_generators:
                            del trade_generators[symbol]

    finally:
        with lock:
            for scheduler in schedulers.values():
                scheduler.stop()


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, debug=True)
