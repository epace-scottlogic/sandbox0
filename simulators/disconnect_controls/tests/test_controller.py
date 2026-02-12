import pytest
from unittest.mock import patch, AsyncMock

from disconnect_controls.controller import DisconnectController
from disconnect_controls.modes import DisconnectMode


class ConcreteDisconnectController(DisconnectController):
    def __init__(self):
        super().__init__()
        self.graceful_called = False
        self.abrupt_called = False
        self.temporary_called = False
        self.temporary_delay = None
        self.reconnect_called = False

    async def on_graceful_disconnect(self):
        self.graceful_called = True

    async def on_abrupt_disconnect(self):
        self.abrupt_called = True

    async def on_temporary_drop(self, delay_seconds: int):
        self.temporary_called = True
        self.temporary_delay = delay_seconds

    async def on_reconnect(self):
        self.reconnect_called = True


class TestDisconnectController:
    def test_controller_is_abstract(self):
        with pytest.raises(TypeError):
            DisconnectController()

    def test_concrete_controller_can_be_instantiated(self):
        controller = ConcreteDisconnectController()
        assert controller is not None

    def test_controller_has_input_handler(self):
        controller = ConcreteDisconnectController()
        assert controller.input_handler is not None

    def test_controller_running_is_true_initially(self):
        controller = ConcreteDisconnectController()
        assert controller._running is True

    def test_stop_sets_running_to_false(self):
        controller = ConcreteDisconnectController()
        controller.stop()
        assert controller._running is False


class TestDisconnectControllerAsync:
    @pytest.mark.asyncio
    async def test_handle_command_calls_graceful_on_confirmation(self):
        controller = ConcreteDisconnectController()
        with patch.object(
            controller.input_handler,
            "get_confirmation",
            new_callable=AsyncMock,
            return_value=(True, None),
        ):
            await controller.handle_command(DisconnectMode.GRACEFUL)
            assert controller.graceful_called is True

    @pytest.mark.asyncio
    async def test_handle_command_does_not_call_graceful_on_cancel(self):
        controller = ConcreteDisconnectController()
        with patch.object(
            controller.input_handler,
            "get_confirmation",
            new_callable=AsyncMock,
            return_value=(False, None),
        ):
            await controller.handle_command(DisconnectMode.GRACEFUL)
            assert controller.graceful_called is False

    @pytest.mark.asyncio
    async def test_handle_command_calls_abrupt_on_confirmation(self):
        controller = ConcreteDisconnectController()
        with patch.object(
            controller.input_handler,
            "get_confirmation",
            new_callable=AsyncMock,
            return_value=(True, None),
        ):
            await controller.handle_command(DisconnectMode.ABRUPT)
            assert controller.abrupt_called is True

    @pytest.mark.asyncio
    async def test_handle_command_calls_temporary_with_delay(self):
        controller = ConcreteDisconnectController()
        with patch.object(
            controller.input_handler,
            "get_confirmation",
            new_callable=AsyncMock,
            return_value=(True, 30),
        ):
            await controller.handle_command(DisconnectMode.TEMPORARY)
            assert controller.temporary_called is True
            assert controller.temporary_delay == 30

    @pytest.mark.asyncio
    async def test_handle_command_quit_calls_graceful_without_confirmation(self):
        controller = ConcreteDisconnectController()
        await controller.handle_command(DisconnectMode.QUIT)
        assert controller.graceful_called is True

    @pytest.mark.asyncio
    async def test_handle_command_quit_stops_controller(self):
        controller = ConcreteDisconnectController()
        await controller.handle_command(DisconnectMode.QUIT)
        assert controller._running is False

    @pytest.mark.asyncio
    async def test_handle_temporary_sets_should_reconnect_and_stops(self):
        controller = ConcreteDisconnectController()
        with patch.object(
            controller.input_handler,
            "get_confirmation",
            new_callable=AsyncMock,
            return_value=(True, 1),
        ):
            with patch("asyncio.sleep", new_callable=AsyncMock) as mock_sleep:
                await controller.handle_command(DisconnectMode.TEMPORARY)
                mock_sleep.assert_called_once_with(1)
                assert controller._should_reconnect is True
                assert controller._running is False
