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
        SpanSplitEnumerator<char> lineSpanEnumerator = line.Split(':');
        if (!lineSpanEnumerator.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        TodoItem currentTodo = new();
        ReadOnlySpan<char> metaDataSpan = line[lineSpanEnumerator.Current];

        (Classification classification, bool finished) = ParseMetaData(line, metaDataSpan);

        currentTodo.Classification = classification;
        currentTodo.Finished = finished;

        if (!lineSpanEnumerator.MoveNext()) {
            throw new InvalidOperationException($"Missing body : \"{line}\"");
        }
        currentTodo.Body = line[lineSpanEnumerator.Current.Start..].Trim().ToString();

        return currentTodo;
    }
    private static (Classification classification, bool finished) ParseMetaData(ReadOnlySpan<char> line, ReadOnlySpan<char> metaDataSpan) {
        SpanSplitEnumerator<char> metadataPartsSpanEnumerator = metaDataSpan.Split(' ');

        if (!metadataPartsSpanEnumerator.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        if (!Enum.TryParse(metaDataSpan[metadataPartsSpanEnumerator.Current], ignoreCase: true, out Classification classification)) {
            throw new InvalidOperationException($"Unable to parse classification from \"{line}\"");
        }

        if (!metadataPartsSpanEnumerator.MoveNext()) {
            throw new InvalidOperationException($"Unable to parse line : \"{line}\"");
        }

        bool finished = metaDataSpan[metadataPartsSpanEnumerator.Current] switch {
            "_" => false,
            "x" => true,
            _ => throw new InvalidOperationException($"Unable to parse status from \"{line}\"")
        };

        if (metadataPartsSpanEnumerator.MoveNext()) {
            throw new InvalidOperationException($"Metadata parts too long : \"{line}\"");
        }

        return (classification, finished);
    }
}
