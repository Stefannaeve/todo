using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        foreach (ArgumentType argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.Info:
                    Message.InfoBool(true);
                    break;
                case ArgumentType.Verbose:
                    Message.VerboseBool(true);
                    break;
            }
        }

        Condition condition = args[0].ToCondition();
        Message.Debug(condition.ToString());
        MyFile myFile = new MyFile("todo.txt");
        myFile.ParseFile();
        Todo todo = new Todo(myFile);
        bool all = false;

        foreach (ArgumentType argType in args.GetArgumentType()) {
            switch (argType) {
                case ArgumentType.None:
                    throw new InvalidOperationException("No arguments");
                case ArgumentType.Important:
                    Message.ExtraInfo("Add important classification");
                    todo.Classification = Classification.Important;
                    break;
                case ArgumentType.All:
                    Message.ExtraInfo("Add all attribute");
                    all = true;
                    break;
            }
        }

        switch (condition) {
            case Condition.Add:
                Message.ExtraInfo("Doing Adding");
                if (TryFindArgument(args, out string? value)) {
                    todo.body = value;
                    todo.Add();
                    break;
                }
                throw new UnreachableException("Could not find body in arguments");

            case Condition.Delete:
                if (all) {
                    Console.WriteLine("Hwllol");
                    myFile.DeleteAll();
                    break;
                }
                Message.ExtraInfo("Doing Delete");
                if (!TryFindArgument(args, out string? deleteValue) || !int.TryParse(deleteValue, out int deleteIndex)) {
                    throw new ArgumentException("Could not parse deleteIndex");
                }

                if (!todo.Delete(deleteIndex)) {
                    Message.Info($"Could not delete {deleteIndex}");
                }
                break;

            case Condition.Done: {
                if (!TryFindArgument(args, out string? doneValue) || !int.TryParse(doneValue, out int doneIndex)) {
                    throw new ArgumentException("Could not parse doneIndex");
                }

                if (!todo.Finish(doneIndex)) {
                    Message.Info($"Could not finish {doneIndex}");
                }
                break;
            }
            case Condition.Unknown:
                Message.Info($"Doesnt recognize condition {args[0]}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        myFile.Save();
    }

    public static bool TryFindArgument(string[] args, [NotNullWhen(true)] out string? output) {
        for (int i = 0; i < args.Length; i++) {
            string currentArgument = args[i];
            if (i == 0) {
                continue;
            }

            if (currentArgument[0] != '-') {
                output = currentArgument;
                return true;
            }
        }

        output = null;
        return false;
    }
}
