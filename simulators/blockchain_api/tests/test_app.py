import pytest
import json
import time
from blockchain_api.websocket_handler import WebSocketHandler
from blockchain_api.trade_generator import TradeGenerator
from blockchain_api.interval_scheduler import IntervalScheduler


class TestAppIntegration:
    def test_websocket_handler_with_trade_generator(self):
        handler = WebSocketHandler()
        
        subscribe_response = handler.handle_message(json.dumps({
            "action": "subscribe",
            "channel": "trades",
            "symbol": "ETH-USD"
        }))
        response_data = json.loads(subscribe_response)
        assert response_data["event"] == "subscribed"
        assert response_data["symbol"] == "ETH-USD"
        
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        
        update = handler.format_trade_update(trade)
        update_data = json.loads(update)
        
        assert update_data["event"] == "updated"
        assert update_data["channel"] == "trades"
        assert update_data["symbol"] == "ETH-USD"

    def test_full_flow_subscribe_generate_unsubscribe(self):
        handler = WebSocketHandler()
        
        handler.handle_message(json.dumps({
            "action": "subscribe",
            "channel": "trades",
            "symbol": "BTC-USD"
        }))
        
        assert handler.is_subscribed("BTC-USD")
        
        generator = TradeGenerator("BTC-USD")
        trades = [generator.generate_trade() for _ in range(5)]
        
        for trade in trades:
            update = handler.format_trade_update(trade)
            update_data = json.loads(update)
            assert update_data["symbol"] == "BTC-USD"
        
        handler.handle_message(json.dumps({
            "action": "unsubscribe",
            "channel": "trades",
            "symbol": "BTC-USD"
        }))
        
        assert not handler.is_subscribed("BTC-USD")

    def test_multiple_symbols_subscription(self):
        handler = WebSocketHandler()
        
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
        
        eth_generator = TradeGenerator("ETH-USD")
        btc_generator = TradeGenerator("BTC-USD")
        
        eth_trade = eth_generator.generate_trade()
        btc_trade = btc_generator.generate_trade()
        
        eth_update = json.loads(handler.format_trade_update(eth_trade))
        btc_update = json.loads(handler.format_trade_update(btc_trade))
        
        assert eth_update["symbol"] == "ETH-USD"
        assert btc_update["symbol"] == "BTC-USD"

    def test_scheduler_with_trade_generator(self):
        generator = TradeGenerator("ETH-USD")
        scheduler = IntervalScheduler(min_interval=0.01, max_interval=0.02)
        
        trades_received = []
        
        def on_trade():
            trade = generator.generate_trade()
            trades_received.append(trade)
        
        scheduler.start(on_trade)
        time.sleep(0.1)
        scheduler.stop()
        
        assert len(trades_received) >= 1
        for trade in trades_received:
            assert trade["symbol"] == "ETH-USD"
            assert "timestamp" in trade
            assert "price" in trade
            assert "qty" in trade
