namespace todo.HelperClasses;

public class Message {
    private static bool DebugEnabled = false;
    private static bool InfoEnabled = false;

    public static void debug(bool enabled) {
        DebugEnabled = enabled;
    }

    public static void InfoBool(bool enabled) {
        InfoEnabled = enabled;
    }

    public static void Info(string format, params object[] args) {
        MessagePrint("INFO", Color.Blue, format, args);
    }

    public static void ExtraInfo(string format, params object[] args) {
        if (InfoEnabled) {
            MessagePrint("INFO", Color.Blue, format, args);
        }
    }

    public static void Debug(string format, params object[] args) {
        if (DebugEnabled) {
            MessagePrint("DEBUG", Color.Green, format, args);
        }
    }

    private static void MessagePrint(string level, string color, string format, params object[] args) {
        if (DebugEnabled) {
            Console.WriteLine(
                $"{color}[{level}]{Color.Reset} {string.Format(format, args)}"
            );
        }
    }
}