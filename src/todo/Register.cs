using todo.Commands;

namespace todo;

public class Register
{
    //Map av Enum -> Function son lager en ICommand
    public Dictionary<Command, Func<ICommand>> Commands { get; } = new(); 


    // Register en ny command med Command.Enum som key
    public void RegisterCommand<T>(Command command) where T : ICommand, new() // T må være av type ICommand og ha en empty constructor
    {
        Commands.Add(command, () => new T());
    }
    
    public ICommand GetCommand(Command command)
    {
        return Commands[command]();
    }
}
