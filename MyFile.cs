using System.Text;
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
            string[] currentList = current.Split(' ');

            TodoItem currentTodo = new TodoItem();

            if (currentList[1] == "[Important]:") {
                currentTodo.Classification = Classification.Important;
            }
            else {
                currentTodo.Classification = Classification.Regular;
            }

            int findThePlacement = 0;

            if (currentList[2] == "[x]:") {
                currentTodo.Finished = true;
                findThePlacement = 3;
            }
            else {
                currentTodo.Finished = false;
                findThePlacement = 4;
            }

            StringBuilder stringBuilder = new StringBuilder(currentList[findThePlacement] + " ");

            for (int i = findThePlacement + 1; i < currentList.Length; i++) {
                stringBuilder.Append(currentList[i] + " ");
            }

            currentTodo.Body = stringBuilder.ToString();

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
                finishedString = " ";
            }

            _rawLines.Add($"[{i + 1}]: [{current.Classification}]: [{finishedString}]: {current.Body}");
            File.WriteAllLines(_fileName, _rawLines);
        }
    }
}

public class TodoItem {
    public Classification Classification;
    public bool Finished;
    public string Body = "";
}