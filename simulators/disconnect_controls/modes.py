from enum import Enum


class DisconnectMode(Enum):
    GRACEFUL = "g"
    ABRUPT = "a"
    TEMPORARY = "t"
    QUIT = "q"

    @classmethod
    def from_key(cls, key: str) -> "DisconnectMode | None":
        if not key:
            return None

        key_lower = key.lower()

        key_mapping = {
            "g": cls.GRACEFUL,
            "0": cls.GRACEFUL,
            "a": cls.ABRUPT,
            "1": cls.ABRUPT,
            "t": cls.TEMPORARY,
            "2": cls.TEMPORARY,
            "q": cls.QUIT,
        }

        return key_mapping.get(key_lower)

    @property
    def description(self) -> str:
        descriptions = {
            DisconnectMode.GRACEFUL: "Graceful disconnect - send unsubscribe and close cleanly",
            DisconnectMode.ABRUPT: "Abrupt disconnect - terminate connection immediately",
            DisconnectMode.TEMPORARY: "Temporary drop - disconnect then reconnect after delay",
            DisconnectMode.QUIT: "Quit - graceful disconnect and exit program",
        }
        return descriptions[self]

    @property
    def requires_confirmation(self) -> bool:
        return self != DisconnectMode.QUIT
