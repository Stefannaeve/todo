namespace todo.Extensions;

public static class ArgumentTypeExtensions
{
    extension(string[] args)
    {
        public IEnumerable<Argument> GetArgumentType()
        {
            foreach (string argument in args)
            {
                Argument arg = argument switch
                {
                    ("-i" or "--important") =>
                        new Argument(ArgumentType.Important, null),
                    "-a" or "--all" =>
                        new Argument(ArgumentType.All, null),
                    "--info" =>
                        new Argument(ArgumentType.Info, null),
                    "-v" or "--verbose" =>
                        new Argument(ArgumentType.Verbose, null),
                    string a when !a.StartsWith("-") =>
                        new Argument(ArgumentType.Value, argument),
                    _ =>
                        new Argument(ArgumentType.None, null)
                };
                yield return arg;
            }
        }
    }
}

public record Argument(ArgumentType ArgumentType, string? Value);