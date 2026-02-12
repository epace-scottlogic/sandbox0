# Blockchain API Simulator

A mocked WebSocket server that mimics the Blockchain.com Exchange API for the trades channel.

## Overview

This simulator provides a WebSocket endpoint that emulates the Blockchain.com Exchange API behavior for the trades channel. It generates fake trade data at random intervals, allowing you to test applications that consume trade data without connecting to the real exchange.

## Features

- WebSocket endpoint at `/ws` that accepts subscribe/unsubscribe requests
- Supports the `trades` channel for ETH-USD and BTC-USD symbols
- Generates fake trades with random prices, quantities, and sides (buy/sell)
- Emits trade updates at configurable random intervals
- Returns proper `rejected` responses for unsupported channels

## Installation

```bash
cd simulators/blockchain_api
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

## Running the Server

```bash
cd simulators/blockchain_api
source venv/bin/activate
flask run
```

The server will start on `http://localhost:5000` with the WebSocket endpoint at `ws://localhost:5000/ws`.

## API Usage

### Subscribe to Trades

Send a JSON message to subscribe to trade updates:

```json
{"action": "subscribe", "channel": "trades", "symbol": "ETH-USD"}
```

Response:

```json
{"seqnum": 0, "event": "subscribed", "channel": "trades", "symbol": "ETH-USD"}
```

### Trade Updates

After subscribing, you will receive trade updates at random intervals:

```json
{
  "seqnum": 1,
  "event": "updated",
  "channel": "trades",
  "symbol": "ETH-USD",
  "timestamp": "2024-01-15T10:30:45.123456Z",
  "side": "buy",
  "qty": 0.5,
  "price": 2500.50,
  "trade_id": "1705312245123456"
}
```

### Unsubscribe

```json
{"action": "unsubscribe", "channel": "trades", "symbol": "ETH-USD"}
```

Response:

```json
{"seqnum": 2, "event": "unsubscribed", "channel": "trades", "symbol": "ETH-USD"}
```

### Rejected Requests

If you try to subscribe to an unsupported channel:

```json
{"action": "subscribe", "channel": "l2", "symbol": "ETH-USD"}
```

Response:

```json
{"seqnum": 0, "event": "rejected", "text": "Channel 'l2' is not supported"}
```

## Project Structure

```
blockchain_api/
├── blockchain_api/
│   ├── __init__.py
│   ├── app.py                 # Flask application with WebSocket endpoint
│   ├── trade_generator.py     # Generates fake trade data
│   ├── interval_scheduler.py  # Schedules callbacks at random intervals
│   └── websocket_handler.py   # Handles WebSocket message parsing and responses
├── tests/
│   ├── __init__.py
│   ├── test_app.py
│   ├── test_trade_generator.py
│   ├── test_interval_scheduler.py
│   └── test_websocket_handler.py
├── requirements.txt
└── README.md
```

## Running Tests

```bash
cd simulators/blockchain_api
source venv/bin/activate
pytest tests/ -v
```

## Configuration

The trade emission intervals can be configured in `app.py`. By default, trades are emitted between 0.5 and 3.0 seconds apart.
