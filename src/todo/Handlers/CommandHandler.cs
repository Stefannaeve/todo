using todo.Commands;
using todo.Extensions;

namespace todo.Handlers;

public class CommandHandler(MyFile file)
{
    private List<Argument> Arguments { get; } = [];

    public void Handle(ICommand command, string[] args)
    {
        Arguments.AddRange(GetCommonArgs(args));
        Arguments.AddRange(command.ParseArguments(args));
        command.Execute(Arguments, file);
    }

    private static IEnumerable<Argument> GetCommonArgs(string[] args)
    {
        return args.Select(arg => arg switch
        {
            "-i" => new Argument(ArgumentType.Important, null),
            "-v" => new Argument(ArgumentType.Verbose, null),
            _ => arg.GetArgumentType() 
        });
    }
}
