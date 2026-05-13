namespace todo.HelperClasses;

public class Message {
    private static bool InfoEnabled = false;
    private static bool VerboseEnabled = false;

    public static void InfoBool(bool enabled) {
        InfoEnabled = enabled;
    }

    public static void VerboseBool(bool enabled) {
        VerboseEnabled = enabled;
    }

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