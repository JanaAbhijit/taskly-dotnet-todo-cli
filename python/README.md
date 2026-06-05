# Todo app (Python)

A small command-line todo app — a Python port of the .NET solution in the
parent directory. Same behavior, idiomatic Python: a core package (all logic),
a console REPL, and a pytest suite.

## Layout

Dependencies point inward — `cli → service → item`; `item` depends on nothing.

- **`todoapp/item.py`** — the `TodoItem` entity (a dataclass): `id`, `title`,
  `is_done`, `created_at`, and a `__str__` that renders e.g. `[x] #3 buy milk`.
- **`todoapp/service.py`** — `TodoService`: store and operations
  (`add`, `complete`, `reopen`, `remove`, `find`, `get_all`, `get_by_status`,
  `count`). Uses a monotonic id counter and an injectable clock. Accepts an
  optional `store`; when present it loads items on startup and saves after
  every mutation.
- **`todoapp/store.py`** — `JsonTodoStore`: reads/writes items as indented
  JSON (`load()` / `save()`). Datetimes are stored as ISO-8601 strings; a
  missing file loads to an empty list.
- **`todoapp/cli.py`** — thin REPL. Parses a line into command + argument and
  dispatches to `TodoService`. Holds no business rules. Commands: `add`,
  `list`, `done`, `reopen`, `remove`/`rm`, `help`, `quit`/`exit`.
- **`tests/test_todo.py`** — 21 pytest tests against the core package
  (service operations, item rendering, and JSON persistence).

## Commands

```sh
python -m todoapp           # launch the interactive REPL
pip install pytest          # one-time: install the test runner
pytest                      # run all 21 tests (from this directory)
```

Run a single test by name:

```sh
pytest tests/test_todo.py::TestTodoService::test_add_trims_title_and_stamps_clock
```

## Conventions (mirrors the .NET version)

- **Keep logic in the core package, not the CLI.** The CLI only parses input
  and prints output. New behavior goes in `TodoService` so it stays testable.
- **Injectable clock.** `TodoService` takes an optional `Callable[[], datetime]`
  (defaults to `datetime.now(timezone.utc)`); tests pass a fixed clock for
  deterministic `created_at`.
- **Error-handling split.** Invalid input (e.g. empty title) raises
  `ValueError`; "not found" cases return `bool`. The CLI maps the booleans to
  friendly messages.
- **State persists across runs.** The CLI wires up a `JsonTodoStore("todos.json")`,
  so items are loaded on startup and saved after every change. A first run with
  no `todos.json` starts empty (no seeded samples). Without a store, the service
  is in-memory only and behaves exactly as before.
