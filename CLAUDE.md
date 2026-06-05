# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

A small command-line todo app: a .NET 10 solution split into a core library
(all logic), a console REPL, and an xUnit test project.

## Commands

```sh
dotnet build                          # build the whole solution
dotnet test                           # run all 15 xUnit tests
dotnet run --project src/TodoApp.Cli  # launch the interactive REPL
```

Run a single test by name:

```sh
dotnet test --filter "FullyQualifiedName~TodoServiceTests.Add_TrimsTitle_AndStampsClock"
```

## Architecture

Three projects. Dependencies point inward — `Cli → Core` and `Tests → Core`;
`Core` depends on nothing.

- **`src/TodoApp.Core/`** — domain and all business logic, no I/O.
  - `TodoItem.cs` — the entity: `Id`, `Title`, `IsDone`, `CreatedAt`, and a
    `ToString()` that renders e.g. `[x] #3 buy milk`.
  - `TodoService.cs` — in-memory store and operations: `Add`, `Complete`,
    `Reopen`, `Remove`, `Find`, `GetAll`, `GetByStatus`, `Count`. Uses a
    monotonic ID counter.
- **`src/TodoApp.Cli/Program.cs`** — thin top-level-statements REPL. Parses a
  line into command + argument and dispatches to `TodoService`. Holds no
  business rules. Commands: `add`, `list`, `done`, `reopen`, `remove`/`rm`,
  `help`, `quit`/`exit`.
- **`tests/TodoApp.Tests/`** — xUnit tests against `Core`.

## Conventions (follow these)

- **Keep logic in `Core`, not the CLI.** The CLI only parses input and prints
  output. New behavior goes in `TodoService` so it stays unit-testable.
- **Injectable clock.** `TodoService` takes an optional `Func<DateTimeOffset>`
  (defaults to `DateTimeOffset.UtcNow`); tests pass a fixed clock for
  deterministic `CreatedAt`. Preserve this pattern instead of calling
  `DateTimeOffset.UtcNow` directly.
- **Error-handling split.** Invalid input (e.g. empty title) throws
  `ArgumentException`; "not found" cases return `bool`. The CLI maps the
  booleans to friendly messages.
- **State is in-memory only** — nothing persists across runs. The CLI seeds two
  sample items on startup.
- All projects target `net10.0` with `Nullable` and `ImplicitUsings` enabled.

## Running the tests

Run from the repo root (`C:\Work\ClaudeEx`). The test framework is xUnit; the
runner is `dotnet test`.

```sh
dotnet test                                   # build + run all 15 tests
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

A green run ends with: `Passed!  - Failed: 0, Passed: 15, Skipped: 0`.

Collect code coverage (built-in collector):

```sh
dotnet test --collect:"XPlat Code Coverage"   # writes coverage.cobertura.xml under TestResults/
```

## Testing notes

- Tests live in `tests/TodoApp.Tests/TodoServiceTests.cs` (`TodoServiceTests` and
  `TodoItemTests` classes) and target `TodoApp.Core` directly — no CLI involved.
- Tests construct the service with a fixed clock: `new TodoService(() => FixedNow)`,
  so `CreatedAt` assertions are deterministic. Keep this pattern for new tests.
- Use `[Fact]` for single cases and `[Theory]` + `[InlineData]` for parameterized
  ones (see `Add_RejectsEmptyTitle`).
- Coverage includes: ID assignment and non-reuse, title trimming, empty-title
  rejection, complete/reopen/remove (happy and unknown-id paths), status
  filtering, insertion order, and `ToString` formatting.
