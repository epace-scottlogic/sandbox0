import asyncio
import json
import sys
import websockets
import os
from dotenv import load_dotenv
from disconnect_controls import *


load_dotenv()

SYMBOLS = ["ETH-USD", "BTC-USD"]
WEBSOCKET_URL = os.getenv("WEBSOCKET_URL")


def print_separator():
    print("\n" + "=" * 60 + "\n")


def print_json(data: dict, label: str = "Response"):
    print_separator()
    print(f"[{label}]")
    print(json.dumps(data, indent=2))


def create_subscribe_request(symbol: str) -> dict:
    return {"action": "subscribe", "channel": "trades", "symbol": symbol}


def create_unsubscribe_request(symbol: str) -> dict:
    return {"action": "unsubscribe", "channel": "trades", "symbol": symbol}


class WebSocketDisconnectController(DisconnectController):
    def __init__(self, websocket, symbol: str):
        super().__init__()
        self.ws = websocket
        self.symbol = symbol

    async def on_graceful_disconnect(self):
        print("\n[Graceful Disconnect] Sending unsubscribe and closing connection...")
        try:
            unsubscribe_request = create_unsubscribe_request(self.symbol)
            await self.ws.send(json.dumps(unsubscribe_request))
            await asyncio.sleep(0.5)
            await self.ws.close()
            print("[Graceful Disconnect] Connection closed cleanly")
        except Exception as e:
            print(f"[Graceful Disconnect] Error: {e}")

    async def on_abrupt_disconnect(self):
        print("\n[Abrupt Disconnect] Terminating connection immediately...")
        try:
            self.ws.transport.abort()
            print("[Abrupt Disconnect] Connection terminated")
        except Exception as e:
            print(f"[Abrupt Disconnect] Error: {e}")

    async def on_temporary_drop(self, delay_seconds: int):
        print(f"\n[Temporary Drop] Dropping connection for {delay_seconds} seconds...")
        try:
            self.ws.transport.abort()
            print("[Temporary Drop] Connection dropped")
        except Exception as e:
            print(f"[Temporary Drop] Error: {e}")

    async def on_reconnect(self):
        pass


async def connect_and_subscribe_with_controls(symbol: str):
    should_connect = True

    while should_connect:
        should_connect = False
        request = create_subscribe_request(symbol)
        print_json(request, "Sending Request")

        try:
            ws = await websockets.connect(WEBSOCKET_URL)
            await ws.send(json.dumps(request))

            print(f"\nConnected to {WEBSOCKET_URL}")
            print(f"Subscribed to trades for {symbol}")
            print("Listening for trade updates...")
            print_separator()

            controller = WebSocketDisconnectController(ws, symbol)
            controller.input_handler.show_commands()

            async def receive_messages():
                try:
                    while controller._running:
                        try:
                            message = await asyncio.wait_for(
                                controller.ws.recv(), timeout=0.5
                            )
                            data = json.loads(message)
                            if not controller.is_print_paused():
                                print_json(data, "Trade Update")
                        except asyncio.TimeoutError:
                            continue
                        except websockets.exceptions.ConnectionClosed:
                            if controller._running and not controller.is_print_paused():
                                print("\n[Connection lost]")
                            break
                except Exception as e:
                    if controller._running and not controller.is_print_paused():
                        print(f"\n[Receive error] {e}")

            async def handle_input():
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

            await asyncio.gather(receive_messages(), handle_input())

            if controller._should_reconnect:
                print("\n[Reconnect] Re-establishing connection...")
                should_connect = True

        except websockets.exceptions.ConnectionClosed:
            print("\nConnection closed by server")
        except ConnectionRefusedError:
            print(f"\nError: Could not connect to {WEBSOCKET_URL}")
            print("Make sure the Flask server is running with 'flask run'")
        except KeyboardInterrupt:
            print("\n\nDisconnected by user")


async def connect_and_subscribe(symbol: str):
    request = create_subscribe_request(symbol)
    print_json(request, "Sending Request")

    try:
        async with websockets.connect(WEBSOCKET_URL) as ws:
            await ws.send(json.dumps(request))

            print(f"\nConnected to {WEBSOCKET_URL}")
            print(f"Subscribed to trades for {symbol}")
            print("Listening for trade updates... (Press Ctrl+C to stop)")
            print_separator()

            while True:
                message = await ws.recv()
                data = json.loads(message)
                print_json(data, "Trade Update")

    except websockets.exceptions.ConnectionClosed:
        print("\nConnection closed by server")
    except ConnectionRefusedError:
        print(f"\nError: Could not connect to {WEBSOCKET_URL}")
        print("Make sure the Flask server is running with 'flask run'")
    except KeyboardInterrupt:
        print("\n\nDisconnected by user")


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


def show_menu() -> str:
    print("\nOptions:")
    print("  1. Subscribe to trades (with disconnect controls)")
    print("  2. Subscribe to trades (simple mode)")
    print("  3|q. Exit")

    while True:
        choice = input("\nSelect an option (1-3): ").strip()
        if choice in ["1", "2", "3", "q"]:
            return choice
        print("Invalid choice. Please enter 1, 2, 3 or q.")


def main():
    print("=" * 60)
    print("  Blockchain API WebSocket Test Client (Server)")
    print("=" * 60)

    while True:
        choice = show_menu()

        if choice == "1":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...")
            asyncio.run(connect_and_subscribe_with_controls(symbol))
        elif choice == "2":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...")
            asyncio.run(connect_and_subscribe(symbol))
        elif choice == "3" or choice == "q":
            print("\nGoodbye!")
            break


if __name__ == "__main__":
    main()
