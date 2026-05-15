namespace todo;

public static class Color
{
    public const string Reset = "\u001b[0m";
    public const string Red = "\u001b[31m";
    public const string Green = "\u001b[32m";
    public const string Blue = "\u001b[34m";
    public const string Cyan = "\u001b[36m";

    public static string Info()
    {
        return $"{Blue}[INFO]: {Reset}";
    }
}