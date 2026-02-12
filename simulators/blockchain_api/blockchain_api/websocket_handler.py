import json
from typing import Any


class WebSocketHandler:
    def __init__(self):
        self._seqnum = 0
        self._subscribed_symbols: set[str] = set()

    def _next_seqnum(self) -> int:
        seqnum = self._seqnum
        self._seqnum += 1
        return seqnum

    def handle_message(self, message: str) -> str:
        try:
            data = json.loads(message)
        except json.JSONDecodeError:
            return self._create_rejected_response("Invalid JSON format")

        action = data.get("action")
        channel = data.get("channel")
        symbol = data.get("symbol")

        if not channel:
            return self._create_rejected_response("Missing channel field")

        if channel != "trades":
            return self._create_rejected_response(f"Channel '{channel}' is not supported")

        if not symbol:
            return self._create_rejected_response("Missing symbol field")

        if action == "subscribe":
            return self._handle_subscribe(symbol)
        elif action == "unsubscribe":
            return self._handle_unsubscribe(symbol)
        else:
            return self._create_rejected_response(f"Unknown action: {action}")

    def _handle_subscribe(self, symbol: str) -> str:
        self._subscribed_symbols.add(symbol)
        return json.dumps({
            "seqnum": self._next_seqnum(),
            "event": "subscribed",
            "channel": "trades",
            "symbol": symbol
        })

    def _handle_unsubscribe(self, symbol: str) -> str:
        self._subscribed_symbols.discard(symbol)
        return json.dumps({
            "seqnum": self._next_seqnum(),
            "event": "unsubscribed",
            "channel": "trades",
            "symbol": symbol
        })

    def _create_rejected_response(self, text: str) -> str:
        return json.dumps({
            "seqnum": self._next_seqnum(),
            "event": "rejected",
            "text": text
        })

    def is_subscribed(self, symbol: str) -> bool:
        return symbol in self._subscribed_symbols

    def get_subscribed_symbols(self) -> list[str]:
        return list(self._subscribed_symbols)

    def format_trade_update(self, trade: dict[str, Any]) -> str:
        return json.dumps({
            "seqnum": self._next_seqnum(),
            "event": "updated",
            "channel": "trades",
            "symbol": trade["symbol"],
            "timestamp": trade["timestamp"],
            "side": trade["side"],
            "qty": trade["qty"],
            "price": trade["price"],
            "trade_id": trade["trade_id"]
        })
