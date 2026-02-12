from disconnect_controls.modes import DisconnectMode


class TestDisconnectMode:
    def test_graceful_mode_has_correct_value(self):
        assert DisconnectMode.GRACEFUL.value == "g"

    def test_abrupt_mode_has_correct_value(self):
        assert DisconnectMode.ABRUPT.value == "a"

    def test_temporary_mode_has_correct_value(self):
        assert DisconnectMode.TEMPORARY.value == "t"

    def test_quit_mode_has_correct_value(self):
        assert DisconnectMode.QUIT.value == "q"

    def test_from_key_returns_graceful_for_g(self):
        assert DisconnectMode.from_key("g") == DisconnectMode.GRACEFUL

    def test_from_key_returns_graceful_for_0(self):
        assert DisconnectMode.from_key("0") == DisconnectMode.GRACEFUL

    def test_from_key_returns_abrupt_for_a(self):
        assert DisconnectMode.from_key("a") == DisconnectMode.ABRUPT

    def test_from_key_returns_abrupt_for_1(self):
        assert DisconnectMode.from_key("1") == DisconnectMode.ABRUPT

    def test_from_key_returns_temporary_for_t(self):
        assert DisconnectMode.from_key("t") == DisconnectMode.TEMPORARY

    def test_from_key_returns_temporary_for_2(self):
        assert DisconnectMode.from_key("2") == DisconnectMode.TEMPORARY

    def test_from_key_returns_quit_for_q(self):
        assert DisconnectMode.from_key("q") == DisconnectMode.QUIT

    def test_from_key_returns_none_for_invalid_key(self):
        assert DisconnectMode.from_key("x") is None

    def test_from_key_returns_none_for_empty_string(self):
        assert DisconnectMode.from_key("") is None

    def test_from_key_is_case_insensitive(self):
        assert DisconnectMode.from_key("G") == DisconnectMode.GRACEFUL
        assert DisconnectMode.from_key("A") == DisconnectMode.ABRUPT
        assert DisconnectMode.from_key("T") == DisconnectMode.TEMPORARY
        assert DisconnectMode.from_key("Q") == DisconnectMode.QUIT

    def test_description_returns_string_for_all_modes(self):
        for mode in DisconnectMode:
            assert isinstance(mode.description, str)
            assert len(mode.description) > 0

    def test_requires_confirmation_returns_true_for_graceful(self):
        assert DisconnectMode.GRACEFUL.requires_confirmation is True

    def test_requires_confirmation_returns_true_for_abrupt(self):
        assert DisconnectMode.ABRUPT.requires_confirmation is True

    def test_requires_confirmation_returns_true_for_temporary(self):
        assert DisconnectMode.TEMPORARY.requires_confirmation is True

    def test_requires_confirmation_returns_false_for_quit(self):
        assert DisconnectMode.QUIT.requires_confirmation is False
