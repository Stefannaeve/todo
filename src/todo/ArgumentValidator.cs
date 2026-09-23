using todo.Extensions;

namespace todo;

public static class ArgumentValidator
{
    private static readonly Dictionary<Command, List<ArgumentType>> Rules = new()
    {
        { Command.Add, [ArgumentType.Important, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.Delete, [ArgumentType.All, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.Done, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline, ArgumentType.Value] },
        { Command.List, [ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Offline] },
    };

    public static bool validateArguments(Command command, List<Argument> arguments) =>
        GetValidationError(command, arguments) is null;

    public static string? GetValidationError(Command command, List<Argument> arguments)
    {
        if (!Rules.TryGetValue(command, out List<ArgumentType>? allowed))
        {
            return "Unknown command. Use add, delete, done, or list.";
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
        if (command == Command.List)
        {
            return null;
        }

        if (command == Command.Delete && arguments.Any(argument => argument.ArgumentType == ArgumentType.All))
        {
            return values.Count == 0 ? null : "Use either todo delete <index> or todo delete --all, not both.";
        }

        if (values.Count != 1)
        {
            return $"Expected exactly one {(command == Command.Add ? "task text (quote text containing spaces)" : "task index")}. {Usage(command)}";
        }

        if (command == Command.Add)
        {
            return string.IsNullOrWhiteSpace(values[0].Value) ? "Task text cannot be empty. Use: todo add \"task text\"" : null;
        }

        return int.TryParse(values[0].Value, out int index) && index > 0
            ? null
            : $"Task index must be a positive whole number. {Usage(command)}";
    }

    private static string Usage(Command command) => command switch
    {
        Command.Add => "Use: todo add [-i] \"task text\"",
        Command.Delete => "Use: todo delete <index> or todo delete --all",
        Command.Done => "Use: todo done <index>",
        _ => "Use: todo list [--info] [--verbose]",
    };
}
