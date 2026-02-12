import sys
import asyncio
from .modes import DisconnectMode


class InputHandler:
    DEFAULT_DELAY = 5
    MIN_DELAY = 1
    MAX_DELAY = 120

    def __init__(self):
        pass

    def show_commands(self):
        print("\nCommands:")
        print("  [0/g] Graceful disconnect")
        print("  [1/a] Abrupt disconnect")
        print("  [2/t] Temporary drop (reconnect after delay)")
        print("  [q]   Quit")

    def format_confirmation_prompt(self, mode: DisconnectMode) -> str:
        if mode == DisconnectMode.GRACEFUL:
            return f"\n{mode.description}\nPress Enter to confirm, Escape to cancel: "
        elif mode == DisconnectMode.ABRUPT:
            return f"\n{mode.description}\nPress Enter to confirm, Escape to cancel: "
        elif mode == DisconnectMode.TEMPORARY:
            return (
                f"\n{mode.description}\n"
                f"Enter delay in seconds ({self.MIN_DELAY}-{self.MAX_DELAY}) "
                f"or press Enter for default [{self.DEFAULT_DELAY}]: "
            )
        else:
            return ""

    def parse_delay(self, input_str: str) -> int:
        if not input_str:
            return self.DEFAULT_DELAY

        try:
            delay = int(input_str)
            return max(self.MIN_DELAY, min(self.MAX_DELAY, delay))
        except ValueError:
            return self.DEFAULT_DELAY

    async def _read_input_async(self) -> str:
        loop = asyncio.get_event_loop()
        return await loop.run_in_executor(None, self._read_line)

    def _read_line(self) -> str:
        try:
            return input()
        except EOFError:
            return "\x1b"

    async def get_confirmation(self, mode: DisconnectMode) -> tuple[bool, int | None]:
        prompt = self.format_confirmation_prompt(mode)
        print(prompt, end="", flush=True)

        user_input = await self._read_input_async()

        if user_input == "\x1b":
            print("\nCancelled.")
            return (False, None)

        if mode == DisconnectMode.TEMPORARY:
            delay = self.parse_delay(user_input)
            return (True, delay)

        return (True, None)

    async def check_for_command(self) -> DisconnectMode | None:
        if sys.stdin in asyncio.get_event_loop()._selector._fd_to_key:
            return None

        try:
            loop = asyncio.get_event_loop()
            key = await asyncio.wait_for(
                loop.run_in_executor(None, self._read_single_char), timeout=0.1
            )
            if key:
                return DisconnectMode.from_key(key)
        except asyncio.TimeoutError:
            pass
        except Exception:
            pass

        return None

    def _read_single_char(self) -> str:
        try:
            import select

            if select.select([sys.stdin], [], [], 0)[0]:
                return sys.stdin.read(1)
        except Exception:
            pass
        return ""
