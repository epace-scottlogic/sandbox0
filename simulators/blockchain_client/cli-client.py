import asyncio
import json
import logging
import os
import sys
import datetime

from dotenv import load_dotenv
from signalrcore.hub_connection_builder import HubConnectionBuilder
from disconnect_controls import *

load_dotenv()

SYMBOLS = ["ETH-USD", "BTC-USD"]
SIGNALR_URL = os.getenv("SIGNALR_URL")
BLOCKCHAIN_URL = f"{SIGNALR_URL}/blockchain"


def print_separator():
    print("\n" + "=" * 60 + "\n")


def try_parse_json(value: any) -> any:
    if isinstance(value, str):
        try:
            return json.loads(value)
        except json.JSONDecodeError:
            return value
    return value


def print_json(data: any, label: str = "Response") -> None:
    print_separator()
    print(f"[{label}]")

    if isinstance(data, list):
        parsed_items = [try_parse_json(item) for item in data]
        for item in parsed_items:
            print(
                json.dumps(item, indent=2) if isinstance(item, (dict, list)) else item
            )
        return

    # non-list data: try parse once
    parsed = try_parse_json(data)
    if isinstance(parsed, (dict, list)):
        print(json.dumps(parsed, indent=2))
    else:
        print(parsed)


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


def create_malformed_request() -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "subscribe",
        "params": {"channel": "invalid_channel", "symbol": "INVALID-SYMBOL"},
        "id": "bad-request",
    }
    return json.dumps(request)


_current_controller = None


def on_message_received(message):
    if _current_controller and _current_controller.is_print_paused():
        return
    print(f"Recieved at: {datetime.datetime.now()}")
    print_json(message, "Message Received")


def on_error(error):
    print(f"[Error] {error}")


def on_close():
    print("[Connection Closed]")


def on_open():
    print("[Connection Opened]")


def build_hub_connection():
    hub_connection = (
        HubConnectionBuilder()
        .with_url(BLOCKCHAIN_URL)
        .configure_logging(logging_level=logging.CRITICAL)
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
    return hub_connection


class SignalRDisconnectController(DisconnectController):
    def __init__(self, hub_connection, symbol: str):
        super().__init__()
        self.hub = hub_connection
        self.symbol = symbol

    async def on_graceful_disconnect(self):
        print("[Graceful Disconnect] Unsubscribing and closing connection...")
        try:
            unsubscribe_request = create_unsubscribe_request(self.symbol)
            self.hub.send("SendMessage", [unsubscribe_request])
            await asyncio.sleep(1.0)
            if self.hub.transport and self.hub.transport.state == 1:
                print("[Graceful Disconnect] connection not closed by host - closing hub connection manually")
                self.hub.transport.stop()
            else:
                print("[Graceful Disconnect] Connection closed cleanly")
        except Exception as e:
            print(f"[Graceful Disconnect] Error: {e}")

    async def on_abrupt_disconnect(self):
        print("[Abrupt Disconnect] Terminating connection immediately...")
        try:
            if self.hub.transport:
                self.hub.transport.stop()
        except Exception as e:
            print(f"[Abrupt Disconnect] Error: {e}")

    async def on_temporary_drop(self, delay_seconds: int):
        print(f"[Temporary Drop] Dropping connection for {delay_seconds} seconds...")
        try:
            if self.hub.transport:
                self.hub.stop()
            print("[Temporary Drop] Connection dropped")
        except Exception as e:
            print(f"[Temporary Drop] Error: {e}")

    async def on_reconnect(self):
        pass


async def connect_and_subscribe_with_controls(symbol: str):
    global _current_controller
    should_connect = True

    while should_connect:
        should_connect = False
        hub_connection = build_hub_connection()
        controller = SignalRDisconnectController(hub_connection, symbol)
        _current_controller = controller

        try:
            hub_connection.start()
            print(f"Connecting to {BLOCKCHAIN_URL}...")

            await asyncio.sleep(1)

            subscribe_request = create_subscribe_request(symbol)
            print_json(subscribe_request, "Sending Subscribe Request")
            hub_connection.send("SendMessage", [subscribe_request])

            print(f"Subscribed to trades for {symbol}")
            print("Listening for trade updates...")
            print_separator()

            controller.input_handler.show_commands()

            while controller._running:
                user_input = await asyncio.to_thread(
                    lambda: input() if sys.stdin.isatty() else ""
                )
                if user_input:
                    mode = DisconnectMode.from_key(user_input.strip())
                    if mode:
                        await controller.handle_command(mode)
                        if not controller._running:
                            break
                    else:
                        print(f"Unknown command: {user_input}")
                        controller.input_handler.show_commands()

            if controller._should_reconnect:
                print("\n[Reconnect] Re-establishing connection...")
                should_connect = True

        except KeyboardInterrupt:
            print("\nInterrupted by user, performing graceful disconnect...")
            await controller.on_graceful_disconnect()
        except Exception as e:
            print(f"\nError: {e}")
            print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
            hub_connection.stop()


async def connect_and_subscribe(symbol: str):
    hub_connection = build_hub_connection()

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        subscribe_request = create_subscribe_request(symbol)
        print_json(subscribe_request, "Sending Subscribe Request")
        hub_connection.send("SendMessage", [subscribe_request])

        print(f"Subscribed to trades for {symbol}")
        print("Listening for trade updates... (Press Ctrl+C to stop)")
        print_separator()

        while True:
            await asyncio.sleep(1)

    except KeyboardInterrupt:
        print("Unsubscribing and disconnecting...")
        unsubscribe_request = create_unsubscribe_request(symbol)
        hub_connection.send("SendMessage", [unsubscribe_request])
        await asyncio.sleep(0.5)
        hub_connection.stop()
        print("Disconnected by user")
    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


async def send_malformed_request():
    hub_connection = build_hub_connection()

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        malformed_request = create_malformed_request()
        print_json(malformed_request, "Sending Malformed Request")
        hub_connection.send("SendMessage", [malformed_request])

        await asyncio.sleep(2)

        hub_connection.stop()
        print("Disconnected after receiving error response")

    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


def show_menu() -> str:
    print("\nOptions:")
    print("  1. Subscribe to trades (with disconnect controls)")
    print("  2. Subscribe to trades (simple mode)")
    print("  3. Send malformed request (test error handling)")
    print("  4|q. Exit")

    while True:
        choice = input("\nSelect an option (1-4): ").strip()
        if choice in ["1", "2", "3", "4", "q"]:
            return choice
        print("Invalid choice. Please enter 1, 2, 3, 4 or q.")


def main():
    print("=" * 60)
    print("  Blockchain API SignalR Test Client")
    print("=" * 60)

    while True:
        choice = show_menu()

        if choice == "1":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...\n")
            asyncio.run(connect_and_subscribe_with_controls(symbol))
        elif choice == "2":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...\n")
            asyncio.run(connect_and_subscribe(symbol))
        elif choice == "3":
            input("Press Enter to send a malformed request...\n")
            asyncio.run(send_malformed_request())
        elif choice == "4" or choice == "q":
            print("\nGoodbye!")
            break


if __name__ == "__main__":
    main()
