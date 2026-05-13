namespace todo.Extensions;

public static class ClassificationExtenstion {
    extension(string s) {
        public Classification ToClassification() =>
            Enum.TryParse(s, out Classification classification) ? classification : Classification.Unknown;
    }
}