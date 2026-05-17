using todo.Extensions;

namespace todo.Commands;

public interface ICommand
{
    void Execute(ICollection<Argument> arguments, MyFile file);
    
    List<Argument> ParseArguments(string[] args);
}
