"""JSON file persistence for todo items."""

from __future__ import annotations

import dataclasses
import json
from collections.abc import Iterable
from datetime import datetime
from pathlib import Path

from .item import TodoItem


class JsonTodoStore:
    """Persist :class:`TodoItem` objects to a JSON file.

    Datetimes are stored as ISO-8601 strings (``datetime.isoformat``) and parsed
    back with :meth:`datetime.fromisoformat` on load.
    """

    def __init__(self, path: str | Path) -> None:
        """
        :param path: Destination file for the JSON document. May not yet exist.
        """
        self._path = Path(path)

    def load(self) -> list[TodoItem]:
        """Read all items from the backing file.

        :returns: The stored items, or an empty list if the file does not exist.
        :raises ValueError: if the file exists but contains malformed data.
        """
        if not self._path.exists():
            return []

        raw = json.loads(self._path.read_text(encoding="utf-8"))
        return [self._from_dict(entry) for entry in raw]

    def save(self, items: Iterable[TodoItem]) -> None:
        """Write ``items`` to the backing file as indented JSON.

        Replaces any existing content.
        """
        payload = [self._to_dict(item) for item in items]
        self._path.write_text(
            json.dumps(payload, indent=2),
            encoding="utf-8",
        )

    @staticmethod
    def _to_dict(item: TodoItem) -> dict:
        data = dataclasses.asdict(item)
        data["created_at"] = item.created_at.isoformat()
        return data

    @staticmethod
    def _from_dict(entry: dict) -> TodoItem:
        return TodoItem(
            id=entry["id"],
            title=entry["title"],
            created_at=datetime.fromisoformat(entry["created_at"]),
            is_done=entry.get("is_done", False),
        )
