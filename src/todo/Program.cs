using todo.Extensions;
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


        Config? config = IsDevelopment() ? new Config("todo.txt") : ConfigHandler.LoadConfig();

        if (config == null)
        {
            throw new InvalidOperationException("Config not found");
        }

        CommandArgument commandArgument = ArgumentParser.parseArgs(args);

        Message.InfoEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Info);
        Message.VerboseEnabled =
            commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Verbose);

        Git git = new Git(config.TodoPath);

        git.EnsureInitialized();
        git.Pull();

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

        switch (commandArgument.Command)
        {
            case Command.Add:
                Message.ExtraInfo("Doing Adding");
                string? body = commandArgument.Arguments
                    .Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();
                if (body == null)
                {
                    throw new InvalidOperationException("The body of Add command is null");
                }

                Classification classification =
                    commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Important)
                        ? Classification.Important
                        : Classification.Regular;

                myFile.Append(classification, body);
                gitMessage = $"Added new Todo";
                break;

            case Command.Delete:
                if (commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.All))
                {
                    myFile.DeleteAll();
                    gitMessage = "Removed all todos";
                    break;
                }

                string? value = commandArgument.Arguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();

                if (!int.TryParse(value, out int deleteIndex))
                {
                    throw new InvalidOperationException("Could not parse argument into int in delete");
                }

                Message.ExtraInfo("Doing Delete");

                if (!myFile.Delete(deleteIndex))
                {
                    Message.Info($"Could not delete {deleteIndex}");
                }

                gitMessage = $"Deleted index {deleteIndex}";
                break;

            case Command.Done:
            {
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
        git.Push(gitMessage);
    }

    static bool IsDevelopment()
    {
        string? env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }
}