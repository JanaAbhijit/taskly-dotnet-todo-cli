using TodoApp.Core;
using Xunit;

namespace TodoApp.Tests;

public class JsonTodoStoreTests : IDisposable
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _path =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyList()
    {
        var store = new JsonTodoStore(_path);

        var items = store.Load();

        Assert.Empty(items);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var store = new JsonTodoStore(_path);
        var saved = new[]
        {
            new TodoItem(1, "buy milk", FixedNow),
            new TodoItem(2, "walk dog", FixedNow.AddHours(1)) { IsDone = true },
        };

        store.Save(saved);
        var loaded = store.Load();

        Assert.Equal(2, loaded.Count);

        Assert.Equal(1, loaded[0].Id);
        Assert.Equal("buy milk", loaded[0].Title);
        Assert.False(loaded[0].IsDone);
        Assert.Equal(FixedNow, loaded[0].CreatedAt);

        Assert.Equal(2, loaded[1].Id);
        Assert.Equal("walk dog", loaded[1].Title);
        Assert.True(loaded[1].IsDone);
        Assert.Equal(FixedNow.AddHours(1), loaded[1].CreatedAt);
    }

    [Fact]
    public void Save_WritesIndentedJson()
    {
        var store = new JsonTodoStore(_path);

        store.Save(new[] { new TodoItem(1, "task", FixedNow) });
        var json = File.ReadAllText(_path);

        // Indented output spans multiple lines.
        Assert.Contains("\n", json);
        Assert.Contains("\"Title\"", json);
    }

    [Fact]
    public void Service_WithStore_PersistsAcrossInstances()
    {
        var store1 = new JsonTodoStore(_path);
        var first = new TodoService(() => FixedNow, store1);
        var a = first.Add("buy milk");
        first.Add("walk dog");
        first.Complete(a.Id);

        // A brand new service over the same file should see prior items.
        var store2 = new JsonTodoStore(_path);
        var second = new TodoService(() => FixedNow, store2);

        Assert.Equal(2, second.Count);
        var reloaded = second.Find(a.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("buy milk", reloaded!.Title);
        Assert.True(reloaded.IsDone);
        Assert.Equal(FixedNow, reloaded.CreatedAt);
    }

    [Fact]
    public void Service_WithStore_ContinuesIdsFromLoadedMax()
    {
        var store1 = new JsonTodoStore(_path);
        var first = new TodoService(() => FixedNow, store1);
        first.Add("one");   // id 1
        first.Add("two");   // id 2

        var store2 = new JsonTodoStore(_path);
        var second = new TodoService(() => FixedNow, store2);
        var next = second.Add("three");

        Assert.Equal(3, next.Id);
    }

    [Fact]
    public void Service_WithStore_PersistsRemoval()
    {
        var store1 = new JsonTodoStore(_path);
        var first = new TodoService(() => FixedNow, store1);
        var a = first.Add("keep");
        var b = first.Add("drop");
        first.Remove(b.Id);

        var store2 = new JsonTodoStore(_path);
        var second = new TodoService(() => FixedNow, store2);

        Assert.Equal(1, second.Count);
        Assert.NotNull(second.Find(a.Id));
        Assert.Null(second.Find(b.Id));
    }
}
