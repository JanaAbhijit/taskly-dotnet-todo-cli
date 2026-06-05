# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

A small command-line todo app, implemented twice with identical behavior:

- **.NET 10** (repo root) — a solution split into a core library (all logic), a
  console REPL, and an xUnit test project. This is the primary/reference version.
- **Python 3.10+** (`python/`) — an idiomatic port with the same architecture: a
  core package (all logic), a console REPL, and a pytest suite. See
  [`python/README.md`](python/README.md).

Both share the same design: inward-pointing dependencies, an injectable clock,
an injectable JSON store (tasks persist to `todos.json`), the error-handling
split, and 21 tests. When changing behavior, keep the two implementations in sync.

## Commands

```sh
dotnet build                          # build the whole solution
dotnet test                           # run all 21 xUnit tests
dotnet run --project src/TodoApp.Cli  # launch the interactive REPL (saves to ./todos.json)
```

Run a single test by name:

```sh
dotnet test --filter "FullyQualifiedName~TodoServiceTests.Add_TrimsTitle_AndStampsClock"
```

## Architecture

Three projects. Dependencies point inward — `Cli → Core` and `Tests → Core`;
`Core` depends on nothing.

- **`src/TodoApp.Core/`** — domain and all business logic, no console I/O.
  - `TodoItem.cs` — the entity: `Id`, `Title`, `IsDone`, `CreatedAt`, and a
    `ToString()` that renders e.g. `[x] #3 buy milk`. Has a `[JsonConstructor]`
    so `System.Text.Json` can round-trip it.
  - `TodoService.cs` — store and operations: `Add`, `Complete`, `Reopen`,
    `Remove`, `Find`, `GetAll`, `GetByStatus`, `Count`. Uses a monotonic ID
    counter. When given a store, loads on construction and persists after every
    mutation.
  - `ITodoStore.cs` / `JsonTodoStore.cs` — persistence abstraction and its
    `System.Text.Json` implementation (`Load()`/`Save()` to a file path; a
    missing file loads as empty).
- **`src/TodoApp.Cli/Program.cs`** — thin top-level-statements REPL. Parses a
  line into command + argument and dispatches to `TodoService` (wired to
  `new JsonTodoStore("todos.json")`). Holds no business rules. Commands: `add`,
  `list`, `done`, `reopen`, `remove`/`rm`, `help`, `quit`/`exit`.
- **`tests/TodoApp.Tests/`** — xUnit tests against `Core`.

## Conventions (follow these)

- **Keep logic in `Core`, not the CLI.** The CLI only parses input and prints
  output. New behavior goes in `TodoService` so it stays unit-testable.
- **Injectable clock.** `TodoService(Func<DateTimeOffset>? clock = null,
  ITodoStore? store = null)` — clock is the FIRST optional parameter (defaults to
  `DateTimeOffset.UtcNow`); tests pass a fixed clock for deterministic
  `CreatedAt`. Preserve this ordering so existing `new TodoService(() => FixedNow)`
  call sites keep compiling.
- **Injectable store.** Persistence is the same pattern: pass an `ITodoStore` and
  the service loads on construction and saves after each mutation. With no store
  it's pure in-memory (keeps the service unit-testable without touching disk).
- **Error-handling split.** Invalid input (e.g. empty title) throws
  `ArgumentException`; "not found" cases return `bool`. The CLI maps the
  booleans to friendly messages.
- **Persistence.** The CLI saves tasks to `todos.json` in the working directory
  and reloads them on startup; the next id continues from `max(existing id) + 1`.
  `todos.json` is git-ignored (it's user data). No sample items are seeded — a
  first run with no file starts empty.
- All projects target `net10.0` with `Nullable` and `ImplicitUsings` enabled.

## Running the tests

Run from the repo root (`C:\Work\ClaudeEx`). The test framework is xUnit; the
runner is `dotnet test`.

```sh
dotnet test                                   # build + run all 21 tests
dotnet test tests/TodoApp.Tests               # run only this test project
dotnet test --no-build                        # skip rebuild (faster, if already built)
dotnet test -v normal                         # more verbose output
```

Run a subset with `--filter`:

```sh
# one test by full name
dotnet test --filter "FullyQualifiedName~TodoServiceTests.Add_TrimsTitle_AndStampsClock"

# every test in one class
dotnet test --filter "FullyQualifiedName~TodoServiceTests"

# all the Add_* tests
dotnet test --filter "FullyQualifiedName~TodoService.Add"
```

A green run ends with: `Passed!  - Failed: 0, Passed: 21, Skipped: 0`.

Collect code coverage (built-in collector):

```sh
dotnet test --collect:"XPlat Code Coverage"   # writes coverage.cobertura.xml under TestResults/
```

## Testing notes

- Tests live in `tests/TodoApp.Tests/` — `TodoServiceTests` and `TodoItemTests`
  (in `TodoServiceTests.cs`) plus `JsonTodoStoreTests.cs` (persistence) — and
  target `TodoApp.Core` directly, no CLI involved.
- Tests construct the service with a fixed clock: `new TodoService(() => FixedNow)`,
  so `CreatedAt` assertions are deterministic. Keep this pattern for new tests.
- Persistence tests use a unique temp file (`Path.Combine(Path.GetTempPath(),
  Path.GetRandomFileName())`) and delete it afterward — never the real `todos.json`.
- Use `[Fact]` for single cases and `[Theory]` + `[InlineData]` for parameterized
  ones (see `Add_RejectsEmptyTitle`).
- Coverage includes: ID assignment and non-reuse, title trimming, empty-title
  rejection, complete/reopen/remove (happy and unknown-id paths), status
  filtering, insertion order, `ToString` formatting, and JSON save/load round-trip
  + cross-instance persistence.

## Python port (`python/`)

An idiomatic mirror of the .NET solution. Same architecture, dependencies point
inward — `cli → service → item`; `item` depends on nothing.

```sh
cd python
python -m todoapp           # launch the interactive REPL (saves to ./todos.json)
python -m pytest -q         # run all 21 tests (expect: 21 passed)
```

Run a single test by name:

```sh
python -m pytest tests/test_todo.py::TestTodoService::test_add_trims_title_and_stamps_clock
```

Layout (maps 1:1 to the .NET files):

- **`todoapp/item.py`** — `TodoItem`, a `@dataclass`; `__str__` renders
  `[x] #3 buy milk` (was `TodoItem.cs`).
- **`todoapp/service.py`** — `TodoService`: `add`, `complete`, `reopen`,
  `remove`, `find`, `get_all`, `get_by_status`, `count` (was `TodoService.cs`).
- **`todoapp/cli.py`** — thin REPL; same commands and aliases (was `Program.cs`).
- **`todoapp/store.py`** — `JsonTodoStore(path)` with `load()`/`save()` using
  stdlib `json` (was `JsonTodoStore.cs`).
- **`tests/test_todo.py`** — `TestTodoService` / `TestTodoItem` plus
  `TestJsonTodoStore` / `TestServicePersistence`, 21 tests.

Conventions match the .NET version, translated to Python idiom:

- Logic stays in `service.py`, not the CLI.
- Injectable clock is a `Callable[[], datetime] | None`, defaulting to
  `datetime.now(timezone.utc)`; tests pass `lambda: FIXED_NOW`.
- Error-handling split: invalid input raises `ValueError` (the analogue of
  `ArgumentException`); "not found" returns `bool`.
- The empty-title case uses `@pytest.mark.parametrize` (the analogue of
  `[Theory]` + `[InlineData]`).
- Persistence mirrors the .NET `ITodoStore`: `TodoService(clock=None, store=None)`
  loads on init and saves after each mutation; tests use the `tmp_path` fixture.
- Targets Python 3.10+ (uses `X | None` unions and built-in generics); no
  third-party runtime deps, pytest is the only dev dependency.
