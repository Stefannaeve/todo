namespace todo.Extensions;

public static class ArgumentTypeExtensions
{
    extension(string[] args)
    {
        public IEnumerable<Argument> GetArgumentType(Command command)
        {
            foreach (string argument in args)
            {
                Argument arg = argument switch
                {
                    "--important" =>
                        new Argument(ArgumentType.Important, null),
                    "--all" =>
                        new Argument(ArgumentType.All, null),
                    "--info" =>
                        new Argument(ArgumentType.Info, null),
                    "--verbose" =>
                        new Argument(ArgumentType.Verbose, null),
                    string a when !a.StartsWith("--", StringComparison.InvariantCulture) && !a.StartsWith('-') =>
                        new Argument(ArgumentType.Value, argument),
                    string a when a.StartsWith('-') => new Argument(ArgumentTypeRules.ExpandShortHand(command, a), null),
                        _ =>
                            new Argument(ArgumentType.None, null)
                };
                yield return arg;
            }
        }
    }
}

public record Argument(ArgumentType ArgumentType, string? Value);
