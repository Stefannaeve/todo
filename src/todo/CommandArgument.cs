using todo.Extensions;

namespace todo;

public record CommandArgument(Command Command, List<Argument> Arguments);

public sealed class CommandLineException(string message) : Exception(message);

public static class ArgumentParser
{
    public static CommandArgument parseArgs(string[] args)
    {
        Command command = args.Length == 0 ? Command.List : args[0].ToCommand();
        // Enum parsing also accepts numbers; commands must be named explicitly.
        if (command == Command.Unknown ||
            (args.Length > 0 && !string.Equals(args[0], command.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new CommandLineException("Unknown command. Use add, edit, delete, done, list, sync, status, or undo.");
        }

        List<Argument> arguments = command is Command.Add or Command.Edit
            ? ParseTextArguments(args[1..], command == Command.Edit)
            : args.Length > 1 ? args[1..].GetArgumentType().ToList() : [];
        string? error = ArgumentValidator.GetValidationError(command, arguments);
        if (error is not null)
        {
            throw new CommandLineException(error);
        }

        return new CommandArgument(command, arguments);
    }
    private static List<Argument> ParseTextArguments(string[] tokens, bool needsIndex)
    {
        List<Argument> arguments = [];
        int position = 0;
        while (position < tokens.Length && tokens[position].StartsWith('-'))
        {
            if (tokens[position] == "--")
            {
                position++;
                break;
            }
            arguments.AddRange(new[] { tokens[position++] }.GetArgumentType());
        }
        if (needsIndex && position < tokens.Length)
            arguments.Add(new Argument(ArgumentType.Value, tokens[position++]));
        if (position < tokens.Length)
            arguments.Add(new Argument(ArgumentType.Value, string.Join(" ", tokens[position..])));
        return arguments;
    }

}
