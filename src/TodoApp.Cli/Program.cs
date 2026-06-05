using TodoApp.Core;

var store = new JsonTodoStore("todos.json");
var service = new TodoService(store: store);

Console.WriteLine("Simple Todo. Type 'help' for commands, 'quit' to exit.");
PrintList(service);

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null) break; // EOF (e.g. piped input)

    var input = line.Trim();
    if (input.Length == 0) continue;

    var parts = input.Split(' ', 2, StringSplitOptions.TrimEntries);
    var command = parts[0].ToLowerInvariant();
    var arg = parts.Length > 1 ? parts[1] : string.Empty;

    if (command is "quit" or "exit") break;

    if (!HandleCommand(service, command, arg))
        Console.WriteLine($"Unknown command '{command}'. Type 'help'.");
}

Console.WriteLine("Bye.");

static bool HandleCommand(TodoService service, string command, string arg)
{
    switch (command)
    {
        case "help":
            PrintHelp();
            return true;

        case "list":
            PrintList(service);
            return true;

        case "add":
            try
            {
                var item = service.Add(arg);
                Console.WriteLine($"Added {item}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return true;

        case "done":
            ApplyById(arg, id => service.Complete(id), "Completed", "No item with id");
            return true;

        case "reopen":
            ApplyById(arg, id => service.Reopen(id), "Reopened", "No item with id");
            return true;

        case "remove":
        case "rm":
            ApplyById(arg, id => service.Remove(id), "Removed", "No item with id");
            return true;

        default:
            return false;
    }
}

static void ApplyById(string arg, Func<int, bool> action, string okVerb, string failMsg)
{
    if (!int.TryParse(arg, out var id))
    {
        Console.WriteLine($"Expected a numeric id, got '{arg}'.");
        return;
    }

    Console.WriteLine(action(id) ? $"{okVerb} #{id}" : $"{failMsg} {id}.");
}

static void PrintList(TodoService service)
{
    if (service.Count == 0)
    {
        Console.WriteLine("(no items)");
        return;
    }

    foreach (var item in service.GetAll())
        Console.WriteLine($"  {item}");
}

static void PrintHelp()
{
    Console.WriteLine("""
        Commands:
          add <title>     Add a new item
          list            Show all items
          done <id>       Mark item complete
          reopen <id>     Mark item not complete
          remove <id>     Delete an item (alias: rm)
          help            Show this help
          quit            Exit (alias: exit)
        """);
}
