import asyncio
from abc import ABC, abstractmethod

from .modes import DisconnectMode
from .input_handler import InputHandler


class DisconnectController(ABC):
    def __init__(self):
        self.input_handler = InputHandler()
        self._running = True
        self._print_paused = False
        self._should_reconnect = False

    def is_print_paused(self) -> bool:
        return self._print_paused

    @abstractmethod
    async def on_graceful_disconnect(self):
        pass

    @abstractmethod
    async def on_abrupt_disconnect(self):
        pass

    @abstractmethod
    async def on_temporary_drop(self, delay_seconds: int):
        pass

    @abstractmethod
    async def on_reconnect(self):
        pass

    def stop(self):
        self._running = False

    async def handle_command(self, mode: DisconnectMode):
        if mode == DisconnectMode.QUIT:
            self._print_paused = True
            await self.on_graceful_disconnect()
            self.stop()
            return

        if mode.requires_confirmation:
            self._print_paused = True
            confirmed, delay = await self.input_handler.get_confirmation(mode)
            if not confirmed:
                self._print_paused = False
                self.input_handler.show_commands()
                return

        if mode == DisconnectMode.GRACEFUL:
            await self.on_graceful_disconnect()
            self.stop()
        elif mode == DisconnectMode.ABRUPT:
            await self.on_abrupt_disconnect()
            self.stop()
        elif mode == DisconnectMode.TEMPORARY:
            await self.on_temporary_drop(delay)
            for i in range(delay):
                print(f"Reconnecting in {delay - i} seconds...")
                await asyncio.sleep(1)
            self._should_reconnect = True
            self._print_paused = False
            self.stop()

    async def run_input_loop(self):
        self.input_handler.show_commands()
        while self._running:
            mode = await self.input_handler.check_for_command()
            if mode:
                await self.handle_command(mode)
            await asyncio.sleep(0.1)
