using todo.Extensions;

namespace todo;

public record CommandArgument(Command Command, List<Argument> Arguments);

public static class ArgumentParser
{
    public static CommandArgument parseArgs(string[] args)
    {
        Command command = args.Length == 0 ? Command.List : args[0].ToCommand();

        if (command == Command.Unknown)
        {
            throw new InvalidOperationException("Could not parse Command");
        }

        bool status = true;
        List<Argument> terminalArguments = new List<Argument>();

        if (args.Length > 1)
        {
            terminalArguments = args[1..].GetArgumentType().ToList();
            status = ArgumentValidator.validateArguments(command, terminalArguments);
        }


        // Make more explicit in the future
        if (!status)
        {
            throw new InvalidOperationException("Invalid arguments");
        }

        return new CommandArgument(command, terminalArguments);
    }
}