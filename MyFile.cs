using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
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
        foreach (var current in _rawLines) {
            string[] lineParts = current.Split(':', 2);

            if (lineParts.Length != 2) {
                throw new UnreachableException($"Unable to parse line \"{current}\" in {_fileName}");
            }

            TodoItem currentTodo = new TodoItem();

            currentTodo.Body = lineParts[1].Trim(' ');

            string[] metaDataParts = lineParts[0].Split(' ', 2);
            
            if (metaDataParts.Length != 2) {
                throw new UnreachableException($"Unable to parse meta data from \"{current}\" in {_fileName}");
            }

            Classification classification = metaDataParts[0].ToClassification();
            
            if (classification == Classification.Unknown) {
                throw new UnreachableException($"Unable to parse classification from \"{current} in {_fileName}");
            }

            currentTodo.Classification = classification;

            switch (metaDataParts[1]) {
                case "_":
                    currentTodo.Finished = false;
                    break;
                case "x":
                    currentTodo.Finished = true;
                    break;
                default:
                    throw new UnreachableException($"Unable to parse status from \"{current} in {_fileName}");
            }

            _todoItems.Add(currentTodo);
        }
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