namespace todo.Extensions;

public static class ArgumentTypeExtensions
{
    extension(string[] args)
    {
        public IEnumerable<Argument> GetArgumentType()
        {
            return args.Select(argument => argument.GetArgumentType());
        }
    }


    extension(string arg)
    {
        public Argument GetArgumentType()
        {
            Argument argument = arg switch
            {
                "--important" =>
                    new Argument(ArgumentType.Important, null),
                "--all" =>
                    new Argument(ArgumentType.All, null),
                "--info" =>
                    new Argument(ArgumentType.Info, null),
                "--verbose" =>
                    new Argument(ArgumentType.Verbose, null),
                not null when !arg.StartsWith('-') =>
                    new Argument(ArgumentType.Value, arg),
                _ =>
                    new Argument(ArgumentType.None, null)
            };
            return argument;
        }
    }
}

public record Argument(ArgumentType ArgumentType, string? Value);
