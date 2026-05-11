using System.Diagnostics;
using todo.Extensions;

namespace todo;

internal static class Program {
    private static void Main(string[] args) {
        if (args.Length < 1) {
            HelperClasses.Message.Info("Args less than one, exiting program");
            Environment.Exit(0);
        }

        // I have ExtraInfo things in the next for loop, thats why im doint
        // a extra one here. 2n is not that bad, fuck off
        foreach (var argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.Info:
                    HelperClasses.Message.InfoBool(true);
                    break;
                case ArgumentType.Verbose:
                    HelperClasses.Message.VerboseBool(true);
                    break;
            }
        }

        var condition = args[0].ToCondition();
        HelperClasses.Message.Debug(condition.ToString());
        var myFile = new MyFile("todo.txt");
        var todo = new Todo(myFile);

        foreach (var argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.None:
                    throw new InvalidOperationException("No arguments");
                case ArgumentType.Important:
                    HelperClasses.Message.ExtraInfo("Add important classification");
                    todo.Classification = Classification.Important;
                    break;
                case ArgumentType.All:
                    HelperClasses.Message.ExtraInfo("Add all attribute");
                    todo.All = true;
                    break;
                case ArgumentType.Info:
                    break;
                case ArgumentType.Verbose:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        switch (condition) {
            case Condition.Add:
                todo.Add("Be Smart");
                break;
            case Condition.Delete:
                HelperClasses.Message.ExtraInfo("Doing Delete");
                todo.Delete();
                break;
            case Condition.Unknown:
                HelperClasses.Message.Info($"Doesnt recognize condition {args[0]}");
                break;
            case Condition.DeleteAll:
                throw new NotImplementedException("Delete all not implemented");
            default:
                throw new ArgumentOutOfRangeException();
        }

        //todo.Add("Ta deg sammen");
    }
}