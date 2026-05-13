using System.Diagnostics;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

public class MyFile(string fileName) {
    private List<TodoItem> _todoItems = [];
    public void ParseFile() {
        if (!File.Exists(fileName)) {
            File.Create(fileName);
        }

        _todoItems = Parser.ParseLines(File.ReadAllLines(fileName)).ToList();
    }

    public List<TodoItem> ParseTodoItemFromLine(List<string> rawLines) {
        List<TodoItem> todoItems = [];
        foreach (string current in rawLines) {
            string[] lineParts = current.Split(':', 2);

            if (lineParts.Length != 2) {
                throw new UnreachableException($"Unable to parse line \"{current}\" in {fileName}");
            }

            TodoItem currentTodo = new TodoItem();

            currentTodo.Body = lineParts[1].Trim(' ');

            string[] metaDataParts = lineParts[0].Split(' ', 2);
            
            if (metaDataParts.Length != 2) {
                throw new UnreachableException($"Unable to parse meta data from \"{current}\" in {fileName}");
            }

            Classification classification = metaDataParts[0].ToClassification();
            
            if (classification == Classification.Unknown) {
                throw new UnreachableException($"Unable to parse classification from \"{current} in {fileName}");
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
                    throw new UnreachableException($"Unable to parse status from \"{current} in {fileName}");
            }

            todoItems.Add(currentTodo);
        }
        
        return todoItems;
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
        if (index < 1 || index - 1 > _todoItems.Count) {
            Message.Info("Could not find the right index, shutting down");
            Environment.Exit(0);
        }

        _todoItems[index - 1].Finished = finished;

        WriteTodosToFile();
    }

    public void Delete(int index) {
        if (index < 1 || index - 1 > _todoItems.Count) {
            Message.Info("Could not find the right index, shutting down");
            Environment.Exit(0);
        }

        _todoItems.RemoveAt(index - 1);
        WriteTodosToFile();
    }

    public void DeleteAll() {
        Message.Debug($"Filename: {fileName}");
        Message.Debug($"Full path: {Path.GetFullPath(fileName)}");
        Message.Debug("DeleteAll");
        File.WriteAllText(fileName, string.Empty);
    }

    public void WriteTodosToFile() {
        List<string> rawLines = [];
        foreach (TodoItem current in _todoItems) {
            string finishedString = "x";
            if (!current.Finished) {
                finishedString = "_";
            }

            rawLines.Add($"{current.Classification} {finishedString}: {current.Body}");
            File.WriteAllLines(fileName, rawLines);
        }
    }
}

public class TodoItem {
    public Classification Classification;
    public bool Finished;
    public string Body = "";
}