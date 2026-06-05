using System.Text.Json;

namespace TodoApp.Core;

/// <summary>
/// <see cref="ITodoStore"/> backed by a JSON file using
/// <see cref="System.Text.Json"/>. Writes indented JSON; a missing file loads
/// to an empty list rather than throwing.
/// </summary>
public class JsonTodoStore : ITodoStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;

    /// <param name="path">File path the items are read from and written to.</param>
    /// <exception cref="ArgumentException">Path is null or whitespace.</exception>
    public JsonTodoStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.", nameof(path));

        _path = path;
    }

    /// <inheritdoc />
    public IReadOnlyList<TodoItem> Load()
    {
        if (!File.Exists(_path))
            return Array.Empty<TodoItem>();

        var json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<TodoItem>();

        var items = JsonSerializer.Deserialize<List<TodoItem>>(json, Options);
        return items ?? new List<TodoItem>();
    }

    /// <inheritdoc />
    public void Save(IEnumerable<TodoItem> items)
    {
        var json = JsonSerializer.Serialize(items, Options);
        File.WriteAllText(_path, json);
    }
}
