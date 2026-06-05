namespace TodoApp.Core;

/// <summary>A single todo entry.</summary>
public class TodoItem
{
    public int Id { get; init; }
    public string Title { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset CreatedAt { get; init; }

    public TodoItem(int id, string title, DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        CreatedAt = createdAt;
    }

    public override string ToString()
    {
        var box = IsDone ? "[x]" : "[ ]";
        return $"{box} #{Id} {Title}";
    }
}
