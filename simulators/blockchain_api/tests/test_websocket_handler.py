import pytest
import json
from unittest.mock import Mock, patch, MagicMock
from blockchain_api.websocket_handler import WebSocketHandler


class TestWebSocketHandler:
    def test_init(self):
        handler = WebSocketHandler()
        assert handler is not None

    def test_handle_subscribe_trades_valid_request(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }
        
        response = handler.handle_message(json.dumps(request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "subscribed"
        assert response_data["channel"] == "trades"
        assert response_data["symbol"] == "ETH-USD"
        assert "seqnum" in response_data

    def test_handle_subscribe_trades_btc_symbol(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "BTC-USD"
        }
        
        response = handler.handle_message(json.dumps(request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "subscribed"
        assert response_data["channel"] == "trades"
        assert response_data["symbol"] == "BTC-USD"

    def test_handle_subscribe_non_trades_channel_rejected(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "channel": "l2",
            "symbol": "ETH-USD"
        }
        
        response = handler.handle_message(json.dumps(request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "rejected"
        assert "text" in response_data

    def test_handle_subscribe_missing_channel_rejected(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "symbol": "ETH-USD"
        }
        
        response = handler.handle_message(json.dumps(request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "rejected"

    def test_handle_subscribe_missing_symbol_rejected(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "channel": "trades"
        }
        
        response = handler.handle_message(json.dumps(request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "rejected"

    def test_handle_invalid_json_rejected(self):
        handler = WebSocketHandler()
        
        response = handler.handle_message("invalid json")
        response_data = json.loads(response)
        
        assert response_data["event"] == "rejected"

    def test_handle_unsubscribe_trades(self):
        handler = WebSocketHandler()
        subscribe_request = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }
        handler.handle_message(json.dumps(subscribe_request))
        
        unsubscribe_request = {
            "action": "unsubscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }
        response = handler.handle_message(json.dumps(unsubscribe_request))
        response_data = json.loads(response)
        
        assert response_data["event"] == "unsubscribed"
        assert response_data["channel"] == "trades"
        assert response_data["symbol"] == "ETH-USD"

    def test_seqnum_increments(self):
        handler = WebSocketHandler()
        request = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }
        
        response1 = handler.handle_message(json.dumps(request))
        response1_data = json.loads(response1)
        
        request2 = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "BTC-USD"
        }
        response2 = handler.handle_message(json.dumps(request2))
        response2_data = json.loads(response2)
        
        assert response2_data["seqnum"] > response1_data["seqnum"]

    def test_is_subscribed_to_symbol(self):
        handler = WebSocketHandler()
        
        assert handler.is_subscribed("ETH-USD") is False
        
        request = {
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }
        handler.handle_message(json.dumps(request))
        
        assert handler.is_subscribed("ETH-USD") is True
        assert handler.is_subscribed("BTC-USD") is False

    def test_get_subscribed_symbols(self):
        handler = WebSocketHandler()
        
        assert handler.get_subscribed_symbols() == []
        
        handler.handle_message(json.dumps({
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }))
        handler.handle_message(json.dumps({
            "action": "subscribe",
            "channel": "trades",
            "symbol": "BTC-USD"
        }))
        
        symbols = handler.get_subscribed_symbols()
        assert "ETH-USD" in symbols
        assert "BTC-USD" in symbols

    def test_format_trade_update(self):
        handler = WebSocketHandler()
        trade = {
            "symbol": "ETH-USD",
            "timestamp": "2019-08-13T11:30:06.100140Z",
            "side": "sell",
            "qty": 8.5e-5,
            "price": 11252.4,
            "trade_id": "12884909920"
        }
        
        update = handler.format_trade_update(trade)
        update_data = json.loads(update)
        
        assert update_data["event"] == "updated"
        assert update_data["channel"] == "trades"
        assert update_data["symbol"] == "ETH-USD"
        assert update_data["timestamp"] == "2019-08-13T11:30:06.100140Z"
        assert update_data["side"] == "sell"
        assert update_data["qty"] == 8.5e-5
        assert update_data["price"] == 11252.4
        assert update_data["trade_id"] == "12884909920"
        assert "seqnum" in update_data
