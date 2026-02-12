import random
from datetime import datetime, timezone


class TradeGenerator:
    def __init__(self, symbol: str):
        self.symbol = symbol
        self._trade_counter = 0

    def generate_trade(self) -> dict:
        self._trade_counter += 1
        timestamp = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ")
        side = random.choice(["buy", "sell"])
        qty = round(random.uniform(0.0001, 10.0), 8)
        price = round(random.uniform(100.0, 100000.0), 2)
        trade_id = str(int(datetime.now(timezone.utc).timestamp() * 1000000) + self._trade_counter)

        return {
            "symbol": self.symbol,
            "timestamp": timestamp,
            "side": side,
            "qty": qty,
            "price": price,
            "trade_id": trade_id,
        }

    @staticmethod
    def format_trade(trade: dict) -> str:
        return f"{trade['symbol']} {trade['side'].upper()} {trade['qty']:.8f} @ ${trade['price']:.2f} (id: {trade['trade_id']})"
