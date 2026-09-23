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
            throw new CommandLineException("Unknown command. Use add, delete, done, list, sync, or status.");
        }

        List<Argument> arguments = args.Length > 1 ? args[1..].GetArgumentType().ToList() : [];
        string? error = ArgumentValidator.GetValidationError(command, arguments);
        if (error is not null)
        {
            throw new CommandLineException(error);
        }

        return new CommandArgument(command, arguments);
    }
}
