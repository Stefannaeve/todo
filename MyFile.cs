using System.Diagnostics;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

public class MyFile {
    private readonly string _fileName;
    private List<string> _rawLines;

    private List<TodoItem> _todoItems = new List<TodoItem>();

    public MyFile(string fileName) {
        _fileName = fileName;
        if (!File.Exists(_fileName)) {
            File.Create(_fileName);
        }

        _rawLines = File.ReadAllLines(_fileName).ToList();
        foreach (var line in _rawLines) {
            _todoItems.Add(ParseLine(line));
        }
    }

    private TodoItem ParseLine(string line) {
        void Fail(string part) =>
            throw new UnreachableException($"Unable to parse {part} from \"{line}\" in {_fileName}");

        string[] parts = line.Split(':', 2);
        if (parts.Length != 2) {
            Fail("line");
        }

        string[] meta = parts[0].Split(' ', 2);
        if (meta.Length != 2) {
            Fail("meta data");
        }

        var classification = meta[0].ToClassification();
        if (classification == Classification.Unknown) {
            Fail("classification"); 
        }

        return new TodoItem {
            Body = parts[1].Trim(),
            Classification = classification,
            Finished = meta[1] switch {
                "_" => false,
                "x" => true,
                _ => throw new UnreachableException($"Unable to parse status from \"{line}\" in {_fileName}")
            }
        };
    }

    public void Append(Classification classification, string body) {
        TodoItem item = new TodoItem();
        item.Finished = false;
        item.Body = body;
        item.Classification = classification;

        _todoItems.Add(item);

        WriteTodosToFile();
    }

    public void UpdateFinished(int index, bool finished) {
        if (index < 1 || index - 1 > _rawLines.Count) {
            Message.Info("Could not find the right index, shutting down");
            Environment.Exit(0);
        }

        _todoItems[index - 1].Finished = finished;

        WriteTodosToFile();
    }

    public void Delete(int index) {
        if (index < 1 || index - 1 > _rawLines.Count) {
            Message.Info("Could not find the right index, shutting down");
            Environment.Exit(0);
        }

        _todoItems.RemoveAt(index - 1);
        WriteTodosToFile();
    }

    public void DeleteAll() {
        Message.Debug($"Filename: {_fileName}");
        Message.Debug($"Full path: {Path.GetFullPath(_fileName)}");
        Message.Debug("DeleteAll");
        File.WriteAllText(_fileName, string.Empty);
    }

    public void WriteTodosToFile() {
        _rawLines.Clear();
        for (int i = 0; i < _todoItems.Count; i++) {
            TodoItem current = _todoItems[i];
            string finishedString = "x";
            if (!current.Finished) {
                finishedString = "_";
            }

            _rawLines.Add($"{current.Classification} {finishedString}: {current.Body}");
            File.WriteAllLines(_fileName, _rawLines);
        }
    }
}

public class TodoItem {
    public Classification Classification;
    public bool Finished;
    public string Body = "";
}