using todo.Extensions;
using todo.HelperClasses;

namespace todo.Commands;

public class AddCommand : ICommand
{

    public void Execute(ICollection<Argument> arguments, MyFile file)
    {
        Message.ExtraInfo("Doing Adding");
        string? body = arguments
            .Where(argument => argument.ArgumentType == ArgumentType.Value)
            .Select(argument => argument.Value)
            .FirstOrDefault();
        if (body == null)
        {
            throw new InvalidOperationException("The body of Add command is null");
        }

        Classification classification =
            arguments.Any(argument => argument.ArgumentType == ArgumentType.Important)
                ? Classification.Important
                : Classification.Regular;


        file.Append(classification, body);
        Console.WriteLine($"{body}: {classification}");
    }

    public List<Argument> ParseArguments(string[] args) => args.Select(ShortHand).ToList();

    private static Argument ShortHand(string arg) => arg switch
    {
        "-i" => new Argument(ArgumentType.Important, null),
        "-a" => new Argument(ArgumentType.All, null),
        _ => new Argument(ArgumentType.None, null)
    };
}

