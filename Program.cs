namespace todo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello world!");
        if (args.Length < 1)
        {
            Console.WriteLine("Args less than one, exiting program");
            Environment.Exit(0);
        }

        Condition? condition = args[0].ToCondition();
        MyFile myFile = new MyFile("todo.txt");
        Todo todo = new Todo(myFile);

        CheckArguments(todo, args);

        switch (condition) {
            case Condition.add:
                todo.add("Be Smart");
                break;
            case Condition.delete:
                Console.WriteLine("Doing Delete");
                todo.delete();
                break;
            default:
                Console.WriteLine("Doesnt recognize condition");
                break;
        }

        todo.add("Ta deg sammen");
        
    }

    private static void CheckArguments(Todo todo, string[] args) {
        foreach (var argument in args) {
            switch (argument) {
                case "-i":
                    Console.WriteLine("Add important classification");
                    todo.classification = Classification.Important;
                    break;
                case "-a" :
                    Console.WriteLine("Add all attribute");
                    todo.all = true;
                    break;
                default:
                    break;
            }
        }
    }
}
