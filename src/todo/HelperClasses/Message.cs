namespace todo.HelperClasses;

public class Message {
    public static bool InfoEnabled { get; set; } = false;
    public static bool VerboseEnabled { get; set; }= false;

    public static void Info(string format, params object[] args) {
        MessagePrint("INFO", Color.Blue, format, args);
    }

    public static void ExtraInfo(string format, params object[] args) {
        if (InfoEnabled || VerboseEnabled) {
            MessagePrint("INFO", Color.Blue, format, args);
        }
    }

    public static void Debug(string format, params object[] args) {
        if (VerboseEnabled) {
            MessagePrint("DEBUG", Color.Green, format, args);
        }
    }

    private static void MessagePrint(string level, string color, string format, params object[] args) {
        Console.WriteLine(
            $"{color}[{level}]{Color.Reset} {string.Format(format, args)}"
        );
    }
}