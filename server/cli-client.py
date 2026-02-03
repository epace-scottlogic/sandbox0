import asyncio
import json
import os

from dotenv import load_dotenv
from signalrcore.hub_connection_builder import HubConnectionBuilder

load_dotenv()

SYMBOLS = ["ETH-USD", "BTC-USD"]
SIGNALR_URL = os.getenv("SIGNALR_URL")
BLOCKCHAIN_URL = f"{SIGNALR_URL}/blockchain"


def print_separator():
    print("\n" + "=" * 60 + "\n")


def print_json(data: dict | str, label: str = "Response"):
    print_separator()
    print(f"[{label}]")
    if isinstance(data, str):
        try:
            parsed = json.loads(data)
            print(json.dumps(parsed, indent=2))
        except json.JSONDecodeError:
            print(data)
    else:
        print(json.dumps(data, indent=2))


def select_symbol() -> str:
    print("\nAvailable symbols:")
    for i, symbol in enumerate(SYMBOLS, 1):
        print(f"  {i}. {symbol}")

    while True:
        try:
            choice = input("\nSelect a symbol (1 or 2): ").strip()
            index = int(choice) - 1
            if 0 <= index < len(SYMBOLS):
                return SYMBOLS[index]
            print("Invalid choice. Please enter 1 or 2.")
        except ValueError:
            print("Invalid input. Please enter a number.")


def create_subscribe_request(symbol: str) -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "subscribe",
        "params": {"channel": "trades", "symbol": symbol},
        "id": "1",
    }
    return json.dumps(request)


def create_unsubscribe_request(symbol: str) -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "unsubscribe",
        "params": {"channel": "trades", "symbol": symbol},
        "id": "2",
    }
    return json.dumps(request)


def on_message_received(message):
    print_json(message, "Message Received")


def on_error(error):
    print(f"\n[Error] {error}")


def on_close():
    print("\n[Connection Closed]")


def on_open():
    print("\n[Connection Opened]")


async def connect_and_subscribe(symbol: str):
    hub_connection = (
        HubConnectionBuilder()
        .with_url(BLOCKCHAIN_URL)
        .with_automatic_reconnect(
            {
                "type": "raw",
                "keep_alive_interval": 10,
                "reconnect_interval": 5,
                "max_attempts": 5,
            }
        )
        .build()
    )

    hub_connection.on_open(on_open)
    hub_connection.on_close(on_close)
    hub_connection.on_error(on_error)
    hub_connection.on("ReceiveMessage", on_message_received)

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        subscribe_request = create_subscribe_request(symbol)
        print_json(subscribe_request, "Sending Subscribe Request")
        hub_connection.send("SendMessage", [subscribe_request])

        print(f"\nSubscribed to trades for {symbol}")
        print("Listening for trade updates... (Press Ctrl+C to stop)")
        print_separator()

        while True:
            await asyncio.sleep(1)

    except KeyboardInterrupt:
        print("\n\nUnsubscribing and disconnecting...")
        unsubscribe_request = create_unsubscribe_request(symbol)
        hub_connection.send("SendMessage", [unsubscribe_request])
        await asyncio.sleep(0.5)
        hub_connection.stop()
        print("Disconnected by user")
    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


def main():
    print("=" * 60)
    print("  Blockchain API SignalR Test Client")
    print("=" * 60)

    symbol = select_symbol()
    print(f"\nYou selected: {symbol}")
    input("Press Enter to connect and subscribe...")

    asyncio.run(connect_and_subscribe(symbol))


if __name__ == "__main__":
    main()
