namespace todo.Extensions;

public static class ClassificationExtenstion
{
    // ReadOnlySpan<char> e basically en pointer te starten av string 
    extension(ReadOnlySpan<char> span)
    {
        public Classification ToClassification() =>
            Enum.TryParse(span, ignoreCase: true, out Classification classification)
                ? classification
                : Classification.Unknown;
    }
}