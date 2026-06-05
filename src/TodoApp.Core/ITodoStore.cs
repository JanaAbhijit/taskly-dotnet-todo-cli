namespace TodoApp.Core;

/// <summary>
/// Persistence abstraction for todo items. Implementations round-trip the
/// full item collection to and from some backing store (file, database, etc.).
/// </summary>
public interface ITodoStore
{
    /// <summary>
    /// Loads all persisted items. Returns an empty list when nothing has been
    /// saved yet (e.g. a missing backing file). Never returns null.
    /// </summary>
    IReadOnlyList<TodoItem> Load();

    /// <summary>Persists the supplied items, replacing any previous contents.</summary>
    void Save(IEnumerable<TodoItem> items);
}
