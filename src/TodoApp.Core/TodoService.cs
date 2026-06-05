namespace TodoApp.Core;

/// <summary>
/// In-memory store and operations for todo items.
/// All mutating operations validate input and throw on invalid arguments.
/// </summary>
public class TodoService
{
    private readonly List<TodoItem> _items = new();
    private readonly Func<DateTimeOffset> _clock;
    private readonly ITodoStore? _store;
    private int _nextId = 1;

    /// <param name="clock">
    /// Optional time source, injected for deterministic tests.
    /// Defaults to <see cref="DateTimeOffset.UtcNow"/>.
    /// </param>
    /// <param name="store">
    /// Optional persistence backend. When supplied, existing items are loaded
    /// on construction and every mutating operation is persisted. When null,
    /// the service is purely in-memory.
    /// </param>
    public TodoService(Func<DateTimeOffset>? clock = null, ITodoStore? store = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _store = store;

        if (_store is not null)
        {
            _items.AddRange(_store.Load());
            _nextId = _items.Count == 0 ? 1 : _items.Max(i => i.Id) + 1;
        }
    }

    private void Persist() => _store?.Save(_items);

    /// <summary>Adds a new item and returns it.</summary>
    /// <exception cref="ArgumentException">Title is null or whitespace.</exception>
    public TodoItem Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be empty.", nameof(title));

        var item = new TodoItem(_nextId++, title.Trim(), _clock());
        _items.Add(item);
        Persist();
        return item;
    }

    /// <summary>Returns all items in insertion order.</summary>
    public IReadOnlyList<TodoItem> GetAll() => _items.AsReadOnly();

    /// <summary>Returns items filtered by completion state.</summary>
    public IReadOnlyList<TodoItem> GetByStatus(bool isDone) =>
        _items.Where(i => i.IsDone == isDone).ToList().AsReadOnly();

    /// <summary>Finds an item by id, or null if not found.</summary>
    public TodoItem? Find(int id) => _items.FirstOrDefault(i => i.Id == id);

    /// <summary>Marks an item complete. Returns false if no such item exists.</summary>
    public bool Complete(int id)
    {
        var item = Find(id);
        if (item is null) return false;
        item.IsDone = true;
        Persist();
        return true;
    }

    /// <summary>Marks an item not complete. Returns false if no such item exists.</summary>
    public bool Reopen(int id)
    {
        var item = Find(id);
        if (item is null) return false;
        item.IsDone = false;
        Persist();
        return true;
    }

    /// <summary>Removes an item. Returns false if no such item exists.</summary>
    public bool Remove(int id)
    {
        var item = Find(id);
        if (item is null) return false;
        _items.Remove(item);
        Persist();
        return true;
    }

    /// <summary>Number of items currently stored.</summary>
    public int Count => _items.Count;
}
