"""The TodoItem entity."""

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime


@dataclass
class TodoItem:
    """A single todo entry."""

    id: int
    title: str
    created_at: datetime
    is_done: bool = False

    def __str__(self) -> str:
        box = "[x]" if self.is_done else "[ ]"
        return f"{box} #{self.id} {self.title}"
