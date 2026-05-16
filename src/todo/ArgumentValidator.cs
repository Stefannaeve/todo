using System.Runtime.CompilerServices;
using todo.Extensions;

namespace todo;

public static class ArgumentValidator
{
    public static bool ValidateArguments(Command command, List<Argument> terminalArguments)
    {
        if (terminalArguments.All(argument => argument.ArgumentType == ArgumentType.None))
        {
            return false;
        }

        List<ArgumentType> allowedArgumentTypes = ArgumentTypeRules.GetAllowedArgumentTypes(command);
        foreach (ArgumentType terminalArgumentType in terminalArguments.Select(argument => argument.ArgumentType))
        {
            if (!allowedArgumentTypes.Contains(terminalArgumentType))
            {
                return false;
            }
        }

        return true;
    }
}

public static class ArgumentTypeRules
{
    public static List<ArgumentType> GetAllowedArgumentTypes(Command command)
    {
        List<ArgumentType> allowedArgs = command switch
        {
            Command.Unknown => [],
            Command.Git => [],
            Command.Add => [ArgumentType.All, ArgumentType.Important, ArgumentType.Value],
            Command.Delete => [ArgumentType.All, ArgumentType.Value],
            Command.Done => [ArgumentType.All, ArgumentType.Value],
            Command.List => [],
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };
        allowedArgs.AddRange(GetUniversalArgumentTypes());
        return allowedArgs;
    }

    private static readonly Dictionary<string, ArgumentType> _addShortHands = new()
    {
        { "-i", ArgumentType.Important },
    };

    // _deleteShortHand + _doneShortHand kommer her 


    private static readonly Dictionary<string, ArgumentType> _universalShortHands = new()
    {
        { "-v", ArgumentType.Verbose },
        { "-I", ArgumentType.Info },
    };

    public static List<ArgumentType> GetUniversalArgumentTypes() => [ArgumentType.Verbose, ArgumentType.Info];

    public static ArgumentType ExpandShortHand(Command command, string arg)
    {
        ArgumentType expr = _universalShortHands.TryGetValue(arg, out ArgumentType argumentType) ? argumentType : ArgumentType.None;
        if (expr is ArgumentType.None)
        {
            return expr;
        }
        return command switch
        {
            Command.Git => ArgumentType.None,
            Command.Delete => ArgumentType.None,
            Command.Done => ArgumentType.None,
            Command.List => ArgumentType.None,
            Command.Add => _addShortHands.TryGetValue(arg, out argumentType) ? argumentType : ArgumentType.None,
            Command.Unknown => throw new InvalidOperationException("Unknown command"),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };
    }
}
