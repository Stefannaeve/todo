using todo.Extensions;

namespace todo;

public static class ArgumentValidator
{
    private static readonly Dictionary<Command, List<ArgumentType>> Rules = new()
    {
        { Command.Edit, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.Add, [ArgumentType.Important, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.Delete, [ArgumentType.All, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.Done, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.List, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline] },
        { Command.Sync, [ArgumentType.Info, ArgumentType.Verbose] },
        { Command.Undo, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline] },
        { Command.Status, [ArgumentType.Info, ArgumentType.Verbose] },
    };

    public static bool validateArguments(Command command, List<Argument> arguments) =>
        GetValidationError(command, arguments) is null;

    public static string? GetValidationError(Command command, List<Argument> arguments)
    {
        if (!Rules.TryGetValue(command, out List<ArgumentType>? allowed))
        {
            return "Unknown command. Use add, edit, delete, done, list, sync, status, or undo.";
        }

        if (command == Command.Done && arguments.Any(argument => argument.ArgumentType == ArgumentType.All))
        {
            return "done does not support --all. Use: todo done <index>";
        }

        if (arguments.Any(argument => !allowed.Contains(argument.ArgumentType)))
        {
            return $"Unsupported argument for {command.ToString().ToLowerInvariant()}. {Usage(command)}";
        }

        List<Argument> values = arguments.Where(argument => argument.ArgumentType == ArgumentType.Value).ToList();
        if (command is Command.List or Command.Sync or Command.Status or Command.Undo)
        {
            return null;
        }

        if (command == Command.Edit)
        {
            if (values.Count != 2)
                return "Expected one task index and replacement text. Use: todo edit <index> \"new text\"";
            if (!int.TryParse(values[0].Value, out int editIndex) || editIndex < 1)
                return "Task index must be a positive whole number. Use: todo edit <index> \"new text\"";
            string? text = values[1].Value;
            if (string.IsNullOrWhiteSpace(text) || text.Contains('\r') || text.Contains('\n'))
                return "Replacement text must be nonblank and on one line. Use: todo edit <index> \"new text\"";
            return null;
        }

        if (command == Command.Delete && arguments.Any(argument => argument.ArgumentType == ArgumentType.All))
        {
            return values.Count == 0 ? null : "Use either todo delete <index> or todo delete --all, not both.";
        }

        if (values.Count != 1)
        {
            return $"Expected exactly one {(command == Command.Add ? "task text" : "task index")}. {Usage(command)}";
        }

        if (command == Command.Add)
        {
            string? text = values[0].Value;
            return string.IsNullOrWhiteSpace(text) || text.Contains('\r') || text.Contains('\n')
                ? "Task text must be nonblank and on one line. Use: todo add task text" : null;
        }

        return int.TryParse(values[0].Value, out int index) && index > 0
            ? null
            : $"Task index must be a positive whole number. {Usage(command)}";
    }

    private static string Usage(Command command) => command switch
    {
        Command.Edit => "Use: todo edit <index> \"new text\" [--offline]",
        Command.Add => "Use: todo add [-i] \"task text\"",
        Command.Delete => "Use: todo delete <index> or todo delete --all",
        Command.Done => "Use: todo done <index>",
        Command.Sync => "Use: todo sync [--verbose]",
        Command.Undo => "Use: todo undo [--offline]",
        Command.Status => "Use: todo status",
        _ => "Use: todo list [--info] [--verbose]",
    };
}
