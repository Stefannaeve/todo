using todo.Extensions;
using todo.HelperClasses;

namespace todo;

internal static class Program {
    private static void Main(string[] args) {

        CommandArgument commandArgument = ArgumentParser.parseArgs(args);
        
        Message.InfoEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Info);
        Message.VerboseEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Verbose);
        
        Message.Debug(commandArgument.Command.ToString());
        MyFile myFile = new MyFile("todo.txt");
        myFile.ParseFile();
        
        if (Parser.ParseErrors.Count > 0) {
            foreach (ParseError parseError in Parser.ParseErrors) {
                Message.Info($"{parseError.Message} at line: {parseError.LineIndex}. this line will be deleted");
            }
        }

        switch (commandArgument.Command) {
            case Command.Add:
                Message.ExtraInfo("Doing Adding");
                string? body = commandArgument.Arguments
                    .Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();
                if (body == null) {
                    throw new InvalidOperationException("The body of Add command is null");
                }

                Classification classification =
                    commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Important)
                        ? Classification.Important
                        : Classification.Regular;
                
                myFile.Append(classification, body);
                break;

            case Command.Delete:
                if (commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.All)) {
                    myFile.DeleteAll();
                    break;
                }

                string? value = commandArgument.Arguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
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

                string? doneValue = commandArgument.Arguments.Where(argument => argument.ArgumentType == ArgumentType.Value)
                    .Select(argument => argument.Value)
                    .FirstOrDefault();

                if (!int.TryParse(doneValue, out int doneIndex)) {
                    throw new InvalidOperationException("Could not parse done index");
                }

                if (!myFile.ToggleFinished(doneIndex)) {
                    Message.Info($"Could not finish {doneIndex}");
                }
                break;
            }
            case Command.List: {
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
