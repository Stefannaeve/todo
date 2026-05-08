using System.Diagnostics;
using todo.Extensions;

namespace todo;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Args less than one, exiting program");
            Environment.Exit(0);
        }

        var condition = args[0].ToCondition();
        var myFile = new MyFile("todo.txt");
        var todo = new Todo(myFile);

        foreach (var argType in args.GetArgumentType())
        {
            switch (argType)
            {
                case ArgumentType.None:
                    throw new InvalidOperationException("No arguments");
                case ArgumentType.Important:
                    Console.WriteLine("Add important classification");
                    todo.classification = Classification.Important;
                    break;
                case ArgumentType.All:
                    Console.WriteLine("Add all attribute");
                    todo.all = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        switch (condition)
        {
            case Condition.Add:
                todo.add("Be Smart");
                break;
            case Condition.Delete:
                Console.WriteLine("Doing Delete");
                todo.delete();
                break;
            case Condition.Unknown:
                Console.WriteLine($"Doesnt recognize condition {args[0]}");
                break;
            case Condition.DeleteAll:
                throw new NotImplementedException("Delete all not implemented");
            default:
                throw new UnreachableException("Missing condition");
        }

        todo.add("Ta deg sammen");

    }
}
