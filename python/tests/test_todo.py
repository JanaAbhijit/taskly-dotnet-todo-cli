from datetime import datetime, timezone

import pytest

from todoapp.item import TodoItem
from todoapp.service import TodoService
from todoapp.store import JsonTodoStore

# Fixed clock so created_at is deterministic.
FIXED_NOW = datetime(2026, 1, 1, 12, 0, 0, tzinfo=timezone.utc)


def new_service() -> TodoService:
    return TodoService(lambda: FIXED_NOW)


class TestTodoService:
    def test_add_assigns_incrementing_ids_starting_at_one(self):
        service = new_service()

        first = service.add("a")
        second = service.add("b")

        assert first.id == 1
        assert second.id == 2
        assert service.count == 2

    def test_add_trims_title_and_stamps_clock(self):
        service = new_service()

        item = service.add("  buy milk  ")

        assert item.title == "buy milk"
        assert item.is_done is False
        assert item.created_at == FIXED_NOW

    @pytest.mark.parametrize("title", ["", "   ", None])
    def test_add_rejects_empty_title(self, title):
        service = new_service()

        with pytest.raises(ValueError):
            service.add(title)
        assert service.count == 0

    def test_complete_marks_item_done_and_returns_true(self):
        service = new_service()
        item = service.add("task")

        result = service.complete(item.id)

        assert result is True
        assert service.find(item.id).is_done is True

    def test_complete_returns_false_for_unknown_id(self):
        service = new_service()

        assert service.complete(999) is False

    def test_reopen_clears_done_flag(self):
        service = new_service()
        item = service.add("task")
        service.complete(item.id)

        result = service.reopen(item.id)

        assert result is True
        assert service.find(item.id).is_done is False

    def test_remove_deletes_item_and_returns_true(self):
        service = new_service()
        item = service.add("task")

        result = service.remove(item.id)

        assert result is True
        assert service.count == 0
        assert service.find(item.id) is None

    def test_remove_returns_false_for_unknown_id(self):
        service = new_service()

        assert service.remove(42) is False

    def test_get_by_status_separates_done_and_pending(self):
        service = new_service()
        a = service.add("done one")
        service.add("pending one")
        service.complete(a.id)

        done = service.get_by_status(is_done=True)
        pending = service.get_by_status(is_done=False)

        assert len(done) == 1
        assert done[0].title == "done one"
        assert len(pending) == 1
        assert pending[0].title == "pending one"

    def test_get_all_returns_items_in_insertion_order(self):
        service = new_service()
        service.add("first")
        service.add("second")
        service.add("third")

        titles = [i.title for i in service.get_all()]

        assert titles == ["first", "second", "third"]

    def test_ids_are_not_reused_after_removal(self):
        service = new_service()
        first = service.add("first")
        service.remove(first.id)

        second = service.add("second")

        assert second.id == 2


class TestJsonTodoStore:
    def test_save_then_load_round_trips_fields(self, tmp_path):
        store = JsonTodoStore(tmp_path / "todos.json")
        items = [
            TodoItem(1, "pending task", FIXED_NOW),
            TodoItem(2, "done task", FIXED_NOW, is_done=True),
        ]

        store.save(items)
        loaded = store.load()

        assert loaded == items
        # Spell out the fields that matter for persistence.
        assert loaded[0].is_done is False
        assert loaded[1].is_done is True
        assert loaded[0].created_at == FIXED_NOW
        assert isinstance(loaded[0].created_at, datetime)

    def test_load_missing_file_returns_empty_list(self, tmp_path):
        store = JsonTodoStore(tmp_path / "does_not_exist.json")

        assert store.load() == []

    def test_save_writes_indented_json(self, tmp_path):
        path = tmp_path / "todos.json"
        store = JsonTodoStore(path)

        store.save([TodoItem(1, "task", FIXED_NOW)])

        text = path.read_text(encoding="utf-8")
        assert "\n  " in text  # indent=2 produced nested indentation


class TestServicePersistence:
    def test_second_service_sees_items_saved_by_first(self, tmp_path):
        store = JsonTodoStore(tmp_path / "todos.json")

        first = TodoService(lambda: FIXED_NOW, store=store)
        first.add("buy milk")
        done = first.add("walk dog")
        first.complete(done.id)

        second = TodoService(lambda: FIXED_NOW, store=JsonTodoStore(tmp_path / "todos.json"))

        titles = [i.title for i in second.get_all()]
        assert titles == ["buy milk", "walk dog"]
        assert second.find(done.id).is_done is True

    def test_next_id_continues_after_reload(self, tmp_path):
        store = JsonTodoStore(tmp_path / "todos.json")
        first = TodoService(lambda: FIXED_NOW, store=store)
        first.add("one")
        first.add("two")

        second = TodoService(lambda: FIXED_NOW, store=JsonTodoStore(tmp_path / "todos.json"))
        third = second.add("three")

        assert third.id == 3

    def test_remove_is_persisted(self, tmp_path):
        store = JsonTodoStore(tmp_path / "todos.json")
        first = TodoService(lambda: FIXED_NOW, store=store)
        a = first.add("keep")
        b = first.add("drop")
        first.remove(b.id)

        second = TodoService(lambda: FIXED_NOW, store=JsonTodoStore(tmp_path / "todos.json"))

        ids = [i.id for i in second.get_all()]
        assert ids == [a.id]


class TestTodoItem:
    def test_str_shows_unchecked_box_when_pending(self):
        item = TodoItem(1, "task", datetime.fromtimestamp(0, tz=timezone.utc))

        assert str(item) == "[ ] #1 task"

    def test_str_shows_checked_box_when_done(self):
        item = TodoItem(
            1, "task", datetime.fromtimestamp(0, tz=timezone.utc), is_done=True
        )

        assert str(item) == "[x] #1 task"
