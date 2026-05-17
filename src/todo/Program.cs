using todo.Commands;
using todo.Extensions;
using todo.Handlers;
using todo.HelperClasses;

namespace todo;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (!ConfigHandler.ConfigExists())
        {
            ConfigHandler.CreateConfig();
        }


        Type a = typeof(AddCommand);
        Config? config = IsDevelopment() ? new Config("") : ConfigHandler.LoadConfig();

        if (config == null)
        {
            throw new InvalidOperationException("Config not found");
        }

        CommandArgument commandArgument = ArgumentParser.parseArgs(args);

        Message.InfoEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Info);
        Message.VerboseEnabled =
            commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Verbose);

        // Git git = new Git(config.TodoPath);
        //
        // git.Pull();

        Message.Debug(commandArgument.Command.ToString());
        MyFile myFile = new MyFile(Path.Combine(config.TodoPath, "todo.txt"));
        myFile.ParseFile();


        if (myFile.ParseErrors.Count > 0)
        {
            foreach (ParseError parseError in myFile.ParseErrors)
            {
                Message.Info($"{parseError.Message} at line: {parseError.LineIndex}. this line will be deleted");
            }
        }

        string gitMessage = "";

        Register register = new();
        register.RegisterCommand<AddCommand>(Command.Add);
        register.RegisterCommand<DeleteCommand>(Command.Delete);

        CommandHandler handler = new(myFile);
        
        // om vi gjør det med alle commandene så trenger vi ikkje en switch i det hele tatt bare register.GetCommand -> Handler.Handle så skal det funke for alle
        switch (commandArgument.Command)
        {
            case Command.Add:
            case Command.Delete:
                ICommand command = register.GetCommand(commandArgument.Command);
                handler.Handle(command, args[1..]);
                break;

            case Command.Done:
            {
                // command = new DoneCommand();
                string? doneValue = commandArgument.Arguments
                    .Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();

                if (!int.TryParse(doneValue, out int doneIndex))
                {
                    throw new InvalidOperationException("Could not parse done index");
                }

                if (!myFile.ToggleFinished(doneIndex))
                {
                    Message.Info($"Could not finish {doneIndex}");
                }

                gitMessage = $"Updated status of {doneIndex}";
                break;
            }
            case Command.List:
            {
                myFile.WriteTodos();
                Environment.Exit(0);
                break;
            }
            case Command.Unknown:
                Message.Info($"Doesnt recognize condition {args[0]}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        myFile.Save();
        // git.Push(gitMessage);
    }

    static bool IsDevelopment()
    {
        string? env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
