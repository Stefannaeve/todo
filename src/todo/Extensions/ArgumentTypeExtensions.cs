namespace todo.Extensions;

public static class ArgumentTypeExtensions {
    extension(string[] args) {
        public IEnumerable<ArgumentType> GetArgumentType() {
            foreach (string argument in args) {
                switch (argument) {
                    case "-i" or "-important" or "--important":
                        yield return ArgumentType.Important;
                        break;
                    case "-a" or "-all" or "--all":
                        yield return ArgumentType.All;
                        break;
                    case "-info" or "--info":
                        yield return ArgumentType.Info;
                        break;
                    case "-v" or "-verbose" or "--verbose":
                        yield return ArgumentType.Verbose;
                        break;
                }
            }
        }
    }
}