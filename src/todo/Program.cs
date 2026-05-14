using todo.Extensions;
using todo.HelperClasses;

namespace todo;

internal static class Program {
    private static void Main(string[] args) {
        bool info = false;

        Command command;

        command = args.Length == 0 ? Command.None : args[0].ToCommand();


        if (command == Command.Unknown) {
            throw new InvalidOperationException("Could not parse Command");
        }

        bool status = true;
        List<Argument> terminalArguments = new List<Argument>();

        if (args.Length > 1) {
            terminalArguments = args[1..].GetArgumentType().ToList();
            status = ArgumentValidator.validateArguments(command, terminalArguments);
        }


        // Make more explicit in the future
        if (!status) {
            throw new InvalidOperationException("Invalid arguments");
        }
        
        Message.InfoEnabled = terminalArguments.Any(argument => argument.ArgumentType == ArgumentType.Info);
        Message.VerboseEnabled = terminalArguments.Any(argument => argument.ArgumentType == ArgumentType.Verbose);
        
        Message.Debug(command.ToString());
        MyFile myFile = new MyFile("todo.txt");
        myFile.ParseFile();

        switch (command) {
            case Command.Add:
                Message.ExtraInfo("Doing Adding");
                string? body = terminalArguments
                    .Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();
                if (body == null) {
                    throw new InvalidOperationException("The body of Add command is null");
                }

                Classification classification =
                    terminalArguments.Any(argument => argument.ArgumentType == ArgumentType.Important)
                        ? Classification.Important
                        : Classification.Regular;
                
                myFile.Append(classification, body);
                break;

            case Command.Delete:
                if (terminalArguments.Any(argument => argument.ArgumentType == ArgumentType.All)) {
                    myFile.DeleteAll();
                    break;
                }

                string? value = terminalArguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();

                if (!int.TryParse(value, out int deleteIndex)) {
                    throw new InvalidOperationException("Could not parse argument into int in delete");
                }
                    
                Message.ExtraInfo("Doing Delete");

                if (!myFile.Delete(deleteIndex)) {
                    Message.Info($"Could not delete {deleteIndex}");
                }
                break;

            case Command.Done: {

                string? doneValue = terminalArguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();

                if (!int.TryParse(doneValue, out int doneIndex)) {
                    throw new InvalidOperationException("Could not parse done index");
                }

                if (!myFile.Finish(doneIndex)) {
                    Message.Info($"Could not finish {doneIndex}");
                }
                break;
            }
            case Command.None: {
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
    }
}
