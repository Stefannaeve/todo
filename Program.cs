using System.Diagnostics;
using System.Text;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

internal static class Program {
    private static void Main(string[] args) {
        if (args.Length < 1) {
            Message.Info("Args less than one, exiting program");
            Environment.Exit(0);
        }

        // I have ExtraInfo things in the next for loop, thats why im doint
        // a extra one here. 2n is not that bad, fuck off
        foreach (var argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.Info:
                    Message.InfoBool(true);
                    break;
                case ArgumentType.Verbose:
                    Message.VerboseBool(true);
                    break;
            }
        }

        var condition = args[0].ToCondition();
        Message.Debug(condition.ToString());
        var myFile = new MyFile("todo.txt");
        var todo = new Todo(myFile);

        foreach (var argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.None:
                    throw new InvalidOperationException("No arguments");
                case ArgumentType.Important:
                    Message.ExtraInfo("Add important classification");
                    todo.Classification = Classification.Important;
                    break;
                case ArgumentType.All:
                    Message.ExtraInfo("Add all attribute");
                    todo.All = true;
                    break;
            }
        }

        switch (condition) {
            case Condition.Add:
                findArgument(args, todo, true);
                todo.Add();
                break;
            case Condition.Delete:
                Message.ExtraInfo("Doing Delete");
                findArgument(args, todo, false);
                todo.Delete();
                break;
            case Condition.Unknown:
                Message.Info($"Doesnt recognize condition {args[0]}");
                break;
            case Condition.DeleteAll:
                throw new NotImplementedException("Delete all not implemented");
            default:
                throw new ArgumentOutOfRangeException();
        }

        //todo.Add("Ta deg sammen");
    }

    public static void findArgument(string[] args, Todo todo, bool findBody) {
        for (int i = 0; i < args.Length; i++) {
            string currentArgument = args[i];
            if (i == 0) {
                continue;
            }

            if (currentArgument[0] != '-') {
                if (findBody) {
                    todo.body = currentArgument;
                    break;
                }

                if (int.TryParse(currentArgument, out var result)) {
                    todo.number = result;
                }
                else {
                    Message.Info("Could not parse number, shutting down");
                    Environment.Exit(0);
                }
            }
        }
    }
}