import pytest
from datetime import datetime, timezone
from blockchain_api.trade_generator import TradeGenerator


class TestTradeGenerator:
    def test_init_with_symbol(self):
        generator = TradeGenerator("ETH-USD")
        assert generator.symbol == "ETH-USD"

    def test_init_with_btc_symbol(self):
        generator = TradeGenerator("BTC-USD")
        assert generator.symbol == "BTC-USD"

    def test_generate_trade_returns_dict(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        assert isinstance(trade, dict)

    def test_generate_trade_has_required_fields(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        
        required_fields = ["symbol", "timestamp", "side", "qty", "price", "trade_id"]
        for field in required_fields:
            assert field in trade, f"Missing field: {field}"

    def test_generate_trade_symbol_matches(self):
        generator = TradeGenerator("BTC-USD")
        trade = generator.generate_trade()
        assert trade["symbol"] == "BTC-USD"

    def test_generate_trade_timestamp_is_current(self):
        generator = TradeGenerator("ETH-USD")
        before = datetime.now(timezone.utc)
        trade = generator.generate_trade()
        after = datetime.now(timezone.utc)
        
        timestamp = datetime.fromisoformat(trade["timestamp"].replace("Z", "+00:00"))
        
        assert before <= timestamp <= after

    def test_generate_trade_timestamp_format(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        
        assert trade["timestamp"].endswith("Z")
        datetime.fromisoformat(trade["timestamp"].replace("Z", "+00:00"))

    def test_generate_trade_side_is_buy_or_sell(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        assert trade["side"] in ["buy", "sell"]

    def test_generate_trade_qty_is_positive_number(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        assert isinstance(trade["qty"], (int, float))
        assert trade["qty"] > 0

    def test_generate_trade_price_is_positive_number(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        assert isinstance(trade["price"], (int, float))
        assert trade["price"] > 0

    def test_generate_trade_trade_id_is_string(self):
        generator = TradeGenerator("ETH-USD")
        trade = generator.generate_trade()
        assert isinstance(trade["trade_id"], str)
        assert len(trade["trade_id"]) > 0

    def test_generate_trade_unique_trade_ids(self):
        generator = TradeGenerator("ETH-USD")
        trade_ids = [generator.generate_trade()["trade_id"] for _ in range(100)]
        assert len(set(trade_ids)) == 100, "Trade IDs should be unique"

    def test_generate_multiple_trades(self):
        generator = TradeGenerator("ETH-USD")
        trades = [generator.generate_trade() for _ in range(10)]
        assert len(trades) == 10
        for trade in trades:
            assert trade["symbol"] == "ETH-USD"
