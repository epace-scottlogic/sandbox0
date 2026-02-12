import random
import threading
from typing import Callable


class IntervalScheduler:
    def __init__(self, min_interval: float = 0.1, max_interval: float = 1.0):
        if min_interval < 0 or max_interval < 0:
            raise ValueError("Intervals must be non-negative")
        if min_interval > max_interval:
            raise ValueError("min_interval must be less than or equal to max_interval")

        self.min_interval = min_interval
        self.max_interval = max_interval
        self._running = False
        self._thread: threading.Thread | None = None
        self._stop_event = threading.Event()

    @property
    def is_running(self) -> bool:
        return self._running

    def get_random_interval(self) -> float:
        return random.uniform(self.min_interval, self.max_interval)

    def start(self, callback: Callable[[], None]) -> None:
        if self._running:
            return

        self._running = True
        self._stop_event.clear()
        self._thread = threading.Thread(target=self._run, args=(callback,), daemon=True)
        self._thread.start()

    def stop(self) -> None:
        if not self._running:
            return

        self._running = False
        self._stop_event.set()
        if self._thread is not None:
            self._thread.join(timeout=1.0)
            self._thread = None

    def _run(self, callback: Callable[[], None]) -> None:
        while not self._stop_event.is_set():
            interval = self.get_random_interval()
            if self._stop_event.wait(timeout=interval):
                break
            if not self._stop_event.is_set():
                callback()
