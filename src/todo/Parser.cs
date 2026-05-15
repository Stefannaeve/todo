using todo.Extensions;
using static System.MemoryExtensions;

namespace todo;

public static class Parser {

    public static IReadOnlyCollection<ParseError> ParseErrors { get => _parseErrors.AsReadOnly(); }
    private static List<ParseError> _parseErrors = [];

    private static int _lineIndex = 1;

    public static IEnumerable<TodoItem> ParseLines(IEnumerable<string> lines) {
        foreach (ReadOnlySpan<char> line in lines.Where(line => !string.IsNullOrWhiteSpace(line))) {
            TodoItem? todoItem = ParseLine(line);
            if (todoItem is not null) {
                yield return todoItem;
            }
            _lineIndex++;
        }
    }

    public static TodoItem? ParseLine(ReadOnlySpan<char> line) {
        SpanSplitEnumerator<char> lineSpanEnumerator = line.Split(':');
        if (!lineSpanEnumerator.MoveNext()) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Unable to parse line : \"{line}\"", _lineIndex));
            return null;
        }

        TodoItem currentTodo = new();
        ReadOnlySpan<char> metaDataSpan = line[lineSpanEnumerator.Current];

        (Classification? classification, bool? finished) = ParseMetaData(line, metaDataSpan);
        if (classification is null || finished is null) {
            return null;
        }

        currentTodo.Classification = classification.Value;
        currentTodo.Finished = finished.Value;

        if (!lineSpanEnumerator.MoveNext()) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Missing body : \"{line}\"", _lineIndex));
            return null;
        }
        currentTodo.Body = line[lineSpanEnumerator.Current.Start..].Trim().ToString();

        return currentTodo;
    }
    private static (Classification? classification, bool? finished) ParseMetaData(ReadOnlySpan<char> line, ReadOnlySpan<char> metaDataSpan) {
        SpanSplitEnumerator<char> metadataPartsSpanEnumerator = metaDataSpan.Split(' ');

        if (!metadataPartsSpanEnumerator.MoveNext()) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Unable to parse line : \"{line}\"", _lineIndex));
            return (null, null);
        }

        if (!Enum.TryParse(metaDataSpan[metadataPartsSpanEnumerator.Current], ignoreCase: true, out Classification classification)) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Unable to parse classification from \"{line}\"", _lineIndex));
            return (null, null);
        }

        if (!metadataPartsSpanEnumerator.MoveNext()) {
            _parseErrors.Add(new ParseError(line.ToString(),$"Unable to parse line : \"{line}\"", _lineIndex ));
            return (null, null);
        }

        bool? finished = metaDataSpan[metadataPartsSpanEnumerator.Current] switch {
            "_" => false,
            "x" => true,
            _ => null
        };
        if (finished is null) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Unable to parse status from \"{line}\"", _lineIndex));
            return (null, null);
        }
        if (metadataPartsSpanEnumerator.MoveNext()) {
            _parseErrors.Add(new ParseError(line.ToString(), $"Metadata parts too long : \"{line}\"", _lineIndex));
            return (null, null);
        }

        return (classification, finished);
    }
}

public record ParseError(string Line, string Message, int LineIndex);
