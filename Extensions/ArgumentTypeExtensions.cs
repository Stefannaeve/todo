namespace todo.Extensions;

public static class ArgumentTypeExtensions
{
    extension(string[] args)
    {

        public IEnumerable<ArgumentType> GetArgumentType()
        {
            foreach (var argument in args)
            {
                switch (argument)
                {
                    case "-i" or "--important":
                        yield return ArgumentType.Important;
                        break;
                    case "-a" or "--all":
                        yield return ArgumentType.All;
                        break;
                }
            }
        }
    }
}
