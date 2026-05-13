using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

internal static class Program {
    private static void Main(string[] args) {
        bool info = false;
        
        if (args.Length < 1) {
            Message.Info("Args less than one, exiting program");
            Environment.Exit(0);
        }

        Command command = args[0].ToCommand();

        if (command == Command.Unknown) {
            throw new InvalidOperationException("Could not parse Command");
        }

        List<Argument> terminalArguments = args[1..].GetArgumentType().ToList();

        bool status = ArgumentValidator.validateArguments(command, terminalArguments);

        // Make more explicit in the future
        if (status == false) {
            throw new InvalidOperationException("Invalid arguments");
        }

        // I have ExtraInfo things in the next for loop, thats why im doint
        // a extra one here. 2n is not that bad, fuck off
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
            case Command.Unknown:
                Message.Info($"Doesnt recognize condition {args[0]}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        myFile.Save();
    }
}
