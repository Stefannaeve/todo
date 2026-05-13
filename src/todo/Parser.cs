using todo.Extensions;
using static System.MemoryExtensions;

namespace todo;

public static class Parser {

    public static IEnumerable<TodoItem> ParseLines(IEnumerable<string> lines) {
        foreach (ReadOnlySpan<char> line in lines) {
            yield return ParseLine(line);
        }
    }

    public static TodoItem ParseLine(ReadOnlySpan<char> line) {
        SpanSplitEnumerator<char> lineSpan = line.Split(':');
        if (!lineSpan.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        TodoItem currentTodo = new();
        ReadOnlySpan<char> metaDataSpan = line[lineSpan.Current];

        (Classification classification, bool finished) = ParseMetaData(line, metaDataSpan);

        currentTodo.Classification = classification;
        currentTodo.Finished = finished;

        if (!lineSpan.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }
        currentTodo.Body = line[lineSpan.Current.Start..].Trim().ToString();

        return currentTodo;
    }
    private static (Classification classification, bool finished) ParseMetaData(ReadOnlySpan<char> line, ReadOnlySpan<char> metaDataSpan) {
        SpanSplitEnumerator<char> metadataPartsSpan = metaDataSpan.Split(' ');

        if (!metadataPartsSpan.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        Classification classification = metaDataSpan[metadataPartsSpan.Current].ToClassification();

        if (!metadataPartsSpan.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        bool finished = metaDataSpan[metadataPartsSpan.Current] switch {
            "_" => false,
            "x" => true,
            _ => throw new InvalidOperationException($"Unable to parse status from \"{line}\"")
        };

        if (metadataPartsSpan.MoveNext()) {
            throw new InvalidOperationException($"Metadata parts too long : \"{line}\"");
        }

        return (classification, finished);
    }
}
