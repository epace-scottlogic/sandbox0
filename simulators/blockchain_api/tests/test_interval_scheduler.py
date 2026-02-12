import pytest
import time
import threading
from unittest.mock import Mock, patch
from blockchain_api.interval_scheduler import IntervalScheduler


class TestIntervalScheduler:
    def test_init_with_min_max_interval(self):
        scheduler = IntervalScheduler(min_interval=0.5, max_interval=2.0)
        assert scheduler.min_interval == 0.5
        assert scheduler.max_interval == 2.0

    def test_init_default_intervals(self):
        scheduler = IntervalScheduler()
        assert scheduler.min_interval == 0.1
        assert scheduler.max_interval == 1.0

    def test_init_raises_if_min_greater_than_max(self):
        with pytest.raises(ValueError):
            IntervalScheduler(min_interval=2.0, max_interval=1.0)

    def test_init_raises_if_negative_interval(self):
        with pytest.raises(ValueError):
            IntervalScheduler(min_interval=-0.1, max_interval=1.0)

    def test_get_random_interval_within_bounds(self):
        scheduler = IntervalScheduler(min_interval=0.5, max_interval=1.5)
        for _ in range(100):
            interval = scheduler.get_random_interval()
            assert 0.5 <= interval <= 1.5

    def test_start_calls_callback(self):
        callback = Mock()
        scheduler = IntervalScheduler(min_interval=0.01, max_interval=0.02)
        
        scheduler.start(callback)
        time.sleep(0.1)
        scheduler.stop()
        
        assert callback.call_count >= 1

    def test_stop_stops_scheduler(self):
        callback = Mock()
        scheduler = IntervalScheduler(min_interval=0.01, max_interval=0.02)
        
        scheduler.start(callback)
        time.sleep(0.05)
        scheduler.stop()
        
        call_count_after_stop = callback.call_count
        time.sleep(0.05)
        
        assert callback.call_count == call_count_after_stop

    def test_is_running_property(self):
        scheduler = IntervalScheduler(min_interval=0.1, max_interval=0.2)
        
        assert scheduler.is_running is False
        
        scheduler.start(Mock())
        assert scheduler.is_running is True
        
        scheduler.stop()
        time.sleep(0.05)
        assert scheduler.is_running is False

    def test_start_when_already_running_does_nothing(self):
        callback = Mock()
        scheduler = IntervalScheduler(min_interval=0.1, max_interval=0.2)
        
        scheduler.start(callback)
        scheduler.start(callback)
        
        scheduler.stop()

    def test_stop_when_not_running_does_nothing(self):
        scheduler = IntervalScheduler(min_interval=0.1, max_interval=0.2)
        scheduler.stop()
