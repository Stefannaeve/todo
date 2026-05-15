using todo.Extensions;

namespace todo;

public static class ArgumentValidator {
    private static Dictionary<Command, List<ArgumentType>> _ruleSet = new() {
        { Command.Git, [ArgumentType.Important] },
        { Command.Add, [ArgumentType.Important, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Value] },
        { Command.Delete, [ArgumentType.All, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Value] },
        { Command.Done, [ArgumentType.All, ArgumentType.Info, ArgumentType.Verbose, ArgumentType.Value] },
    };

    private static Dictionary<Command, Dictionary<string, ArgumentType>> _dictionary = new() {
        {
            Command.Git, new() {
                { "-i", ArgumentType.Important },
                { "-v", ArgumentType.Verbose },
            }
        }, {
            Command.Add, new() {
                { "-i", ArgumentType.Important }
            }
        }, {
            Command.Delete, new() {
                { "-i", ArgumentType.Important }
            }
        }
    };

    public static bool validateArguments(Command command, List<Argument> terminalArguments) {
        if (terminalArguments.All(argument => argument.ArgumentType == ArgumentType.None)) {
            return false;
        }

        List<ArgumentType> allowedArgumentTypes = _ruleSet[command];
        foreach (ArgumentType terminalArgumentType in terminalArguments.Select(argument => argument.ArgumentType)) {
            if (!allowedArgumentTypes.Contains(terminalArgumentType)) {
                return false;
            }
        }

        return true;
    }
}