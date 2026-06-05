"""In-memory store and operations for todo items."""

from __future__ import annotations

from collections.abc import Callable
from datetime import datetime, timezone

from .item import TodoItem


class TodoService:
    """In-memory store and operations for todo items.

    All mutating operations validate input and raise on invalid arguments.
    """

    def __init__(self, clock: Callable[[], datetime] | None = None) -> None:
        """
        :param clock: Optional time source, injected for deterministic tests.
            Defaults to ``datetime.now(timezone.utc)``.
        """
        self._items: list[TodoItem] = []
        self._clock = clock or (lambda: datetime.now(timezone.utc))
        self._next_id = 1

    def add(self, title: str) -> TodoItem:
        """Add a new item and return it.

        :raises ValueError: if ``title`` is None or whitespace.
        """
        if not title or not title.strip():
            raise ValueError("Title must not be empty.")

        item = TodoItem(self._next_id, title.strip(), self._clock())
        self._next_id += 1
        self._items.append(item)
        return item

    def get_all(self) -> list[TodoItem]:
        """Return all items in insertion order."""
        return list(self._items)

    def get_by_status(self, is_done: bool) -> list[TodoItem]:
        """Return items filtered by completion state."""
        return [i for i in self._items if i.is_done == is_done]

    def find(self, id: int) -> TodoItem | None:
        """Find an item by id, or None if not found."""
        return next((i for i in self._items if i.id == id), None)

    def complete(self, id: int) -> bool:
        """Mark an item complete. Return False if no such item exists."""
        item = self.find(id)
        if item is None:
            return False
        item.is_done = True
        return True

    def reopen(self, id: int) -> bool:
        """Mark an item not complete. Return False if no such item exists."""
        item = self.find(id)
        if item is None:
            return False
        item.is_done = False
        return True

    def remove(self, id: int) -> bool:
        """Remove an item. Return False if no such item exists."""
        item = self.find(id)
        if item is None:
            return False
        self._items.remove(item)
        return True

    @property
    def count(self) -> int:
        """Number of items currently stored."""
        return len(self._items)
