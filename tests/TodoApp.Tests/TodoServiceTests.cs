using TodoApp.Core;
using Xunit;

namespace TodoApp.Tests;

public class TodoServiceTests
{
    // Fixed clock so CreatedAt is deterministic.
    private static readonly DateTimeOffset FixedNow =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static TodoService NewService() => new(() => FixedNow);

    [Fact]
    public void Add_AssignsIncrementingIds_StartingAtOne()
    {
        var service = NewService();

        var first = service.Add("a");
        var second = service.Add("b");

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Equal(2, service.Count);
    }

    [Fact]
    public void Add_TrimsTitle_AndStampsClock()
    {
        var service = NewService();

        var item = service.Add("  buy milk  ");

        Assert.Equal("buy milk", item.Title);
        Assert.False(item.IsDone);
        Assert.Equal(FixedNow, item.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Add_RejectsEmptyTitle(string? title)
    {
        var service = NewService();

        Assert.Throws<ArgumentException>(() => service.Add(title!));
        Assert.Equal(0, service.Count);
    }

    [Fact]
    public void Complete_MarksItemDone_AndReturnsTrue()
    {
        var service = NewService();
        var item = service.Add("task");

        var result = service.Complete(item.Id);

        Assert.True(result);
        Assert.True(service.Find(item.Id)!.IsDone);
    }

    [Fact]
    public void Complete_ReturnsFalse_ForUnknownId()
    {
        var service = NewService();

        Assert.False(service.Complete(999));
    }

    [Fact]
    public void Reopen_ClearsDoneFlag()
    {
        var service = NewService();
        var item = service.Add("task");
        service.Complete(item.Id);

        var result = service.Reopen(item.Id);

        Assert.True(result);
        Assert.False(service.Find(item.Id)!.IsDone);
    }

    [Fact]
    public void Remove_DeletesItem_AndReturnsTrue()
    {
        var service = NewService();
        var item = service.Add("task");

        var result = service.Remove(item.Id);

        Assert.True(result);
        Assert.Equal(0, service.Count);
        Assert.Null(service.Find(item.Id));
    }

    [Fact]
    public void Remove_ReturnsFalse_ForUnknownId()
    {
        var service = NewService();

        Assert.False(service.Remove(42));
    }

    [Fact]
    public void GetByStatus_SeparatesDoneAndPending()
    {
        var service = NewService();
        var a = service.Add("done one");
        service.Add("pending one");
        service.Complete(a.Id);

        var done = service.GetByStatus(isDone: true);
        var pending = service.GetByStatus(isDone: false);

        Assert.Single(done);
        Assert.Equal("done one", done[0].Title);
        Assert.Single(pending);
        Assert.Equal("pending one", pending[0].Title);
    }

    [Fact]
    public void GetAll_ReturnsItemsInInsertionOrder()
    {
        var service = NewService();
        service.Add("first");
        service.Add("second");
        service.Add("third");

        var titles = service.GetAll().Select(i => i.Title).ToArray();

        Assert.Equal(new[] { "first", "second", "third" }, titles);
    }

    [Fact]
    public void Ids_AreNotReused_AfterRemoval()
    {
        var service = NewService();
        var first = service.Add("first");
        service.Remove(first.Id);

        var second = service.Add("second");

        Assert.Equal(2, second.Id);
    }
}

public class TodoItemTests
{
    [Fact]
    public void ToString_ShowsUncheckedBox_WhenPending()
    {
        var item = new TodoItem(1, "task", DateTimeOffset.UnixEpoch);

        Assert.Equal("[ ] #1 task", item.ToString());
    }

    [Fact]
    public void ToString_ShowsCheckedBox_WhenDone()
    {
        var item = new TodoItem(1, "task", DateTimeOffset.UnixEpoch) { IsDone = true };

        Assert.Equal("[x] #1 task", item.ToString());
    }
}
