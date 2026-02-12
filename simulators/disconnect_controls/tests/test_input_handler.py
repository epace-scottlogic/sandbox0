import pytest
from unittest.mock import patch, AsyncMock

from disconnect_controls.input_handler import InputHandler
from disconnect_controls.modes import DisconnectMode


class TestInputHandler:
    def test_init_creates_handler(self):
        handler = InputHandler()
        assert handler is not None

    def test_show_commands_prints_available_commands(self, capsys):
        handler = InputHandler()
        handler.show_commands()
        captured = capsys.readouterr()
        assert "[0/g]" in captured.out
        assert "[1/a]" in captured.out
        assert "[2/t]" in captured.out
        assert "[q]" in captured.out

    def test_format_confirmation_prompt_for_graceful(self):
        handler = InputHandler()
        prompt = handler.format_confirmation_prompt(DisconnectMode.GRACEFUL)
        assert "graceful" in prompt.lower()
        assert "enter" in prompt.lower()
        assert "escape" in prompt.lower()

    def test_format_confirmation_prompt_for_abrupt(self):
        handler = InputHandler()
        prompt = handler.format_confirmation_prompt(DisconnectMode.ABRUPT)
        assert "abrupt" in prompt.lower()

    def test_format_confirmation_prompt_for_temporary_includes_delay_info(self):
        handler = InputHandler()
        prompt = handler.format_confirmation_prompt(DisconnectMode.TEMPORARY)
        assert "temporary" in prompt.lower()
        assert "delay" in prompt.lower() or "seconds" in prompt.lower()

    def test_parse_delay_returns_default_for_empty_input(self):
        handler = InputHandler()
        delay = handler.parse_delay("")
        assert delay == 5

    def test_parse_delay_returns_parsed_value_for_valid_number(self):
        handler = InputHandler()
        assert handler.parse_delay("10") == 10
        assert handler.parse_delay("30") == 30
        assert handler.parse_delay("120") == 120

    def test_parse_delay_clamps_to_minimum_1(self):
        handler = InputHandler()
        assert handler.parse_delay("0") == 1
        assert handler.parse_delay("-5") == 1

    def test_parse_delay_clamps_to_maximum_120(self):
        handler = InputHandler()
        assert handler.parse_delay("150") == 120
        assert handler.parse_delay("999") == 120

    def test_parse_delay_returns_default_for_non_numeric_input(self):
        handler = InputHandler()
        assert handler.parse_delay("abc") == 5
        assert handler.parse_delay("five") == 5


class TestInputHandlerAsync:
    @pytest.mark.asyncio
    async def test_get_confirmation_returns_true_on_enter(self):
        handler = InputHandler()
        with patch.object(
            handler, "_read_input_async", new_callable=AsyncMock
        ) as mock_read:
            mock_read.return_value = ""
            confirmed, delay = await handler.get_confirmation(DisconnectMode.GRACEFUL)
            assert confirmed is True
            assert delay is None

    @pytest.mark.asyncio
    async def test_get_confirmation_returns_false_on_escape(self):
        handler = InputHandler()
        with patch.object(
            handler, "_read_input_async", new_callable=AsyncMock
        ) as mock_read:
            mock_read.return_value = "\x1b"
            confirmed, delay = await handler.get_confirmation(DisconnectMode.GRACEFUL)
            assert confirmed is False

    @pytest.mark.asyncio
    async def test_get_confirmation_for_temporary_parses_delay(self):
        handler = InputHandler()
        with patch.object(
            handler, "_read_input_async", new_callable=AsyncMock
        ) as mock_read:
            mock_read.return_value = "30"
            confirmed, delay = await handler.get_confirmation(DisconnectMode.TEMPORARY)
            assert confirmed is True
            assert delay == 30

    @pytest.mark.asyncio
    async def test_get_confirmation_for_temporary_uses_default_delay_on_enter(self):
        handler = InputHandler()
        with patch.object(
            handler, "_read_input_async", new_callable=AsyncMock
        ) as mock_read:
            mock_read.return_value = ""
            confirmed, delay = await handler.get_confirmation(DisconnectMode.TEMPORARY)
            assert confirmed is True
            assert delay == 5
