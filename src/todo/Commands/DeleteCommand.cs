using todo.Extensions;
using todo.HelperClasses;

namespace todo.Commands;

internal class DeleteCommand : ICommand
{
    public void Execute(ICollection<Argument> arguments, MyFile file)
    {
        if (arguments.Any(argument => argument.ArgumentType == ArgumentType.All))
        {
            file.DeleteAll();
            return;
        }

        string? value = arguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
            .Select(argument => argument.Value)
            .FirstOrDefault();

        if (!int.TryParse(value, out int deleteIndex))
        {
            throw new InvalidOperationException("Could not parse argument into int in delete");
        }

        Message.ExtraInfo("Doing Delete");

        if (!file.Delete(deleteIndex))
        {
            Message.Info($"Could not delete {deleteIndex}");
        }
    }
    public List<Argument> ParseArguments(string[] args)
    {
        // Kankje vi kan sjekke at Value er en int her? istedet for at ArgumentType.Value har string så har den object. da kan vær command parse verdien de selv ønkser?
        return [];
    }
}
