namespace todo.Extensions;

internal static class CommandExtensions
{
    extension(ReadOnlySpan<char> span)
    {
        public Command ToCommand() =>
            Enum.TryParse(span, ignoreCase: true, out Command cond) ? cond : Command.Unknown;
    }
}