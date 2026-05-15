using System.Diagnostics;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

public class MyFile(string fileName)
{
    private List<TodoItem> _todoItems = [];
    private readonly Parser _parser = new();
    public IReadOnlyCollection<ParseError> ParseErrors => _parser.ParseErrors;

    public void ParseFile()
    {
        if (!File.Exists(fileName))
        {
            FileStream fileStream = File.Create(fileName);
            fileStream.Close();
        }


        IEnumerable<TodoItem> todoItems = _parser.ParseLines(File.ReadAllLines(fileName));

        _todoItems.AddRange(todoItems.OrderBy(argument => argument.Classification));
    }

    public List<TodoItem> ParseTodoItemFromLine(List<string> rawLines)
    {
        List<TodoItem> todoItems = [];
        foreach (string current in rawLines)
        {
            string[] lineParts = current.Split(':', 2);

            if (lineParts.Length != 2)
            {
                throw new UnreachableException($"Unable to parse line \"{current}\" in {fileName}");
            }

            TodoItem currentTodo = new TodoItem();

            currentTodo.Body = lineParts[1].Trim(' ');

            string[] metaDataParts = lineParts[0].Split(' ', 2);

            if (metaDataParts.Length != 2)
            {
                throw new UnreachableException($"Unable to parse meta data from \"{current}\" in {fileName}");
            }

            Classification classification = metaDataParts[0].ToClassification();

            if (classification == Classification.Unknown)
            {
                throw new UnreachableException($"Unable to parse classification from \"{current} in {fileName}");
            }

            currentTodo.Classification = classification;

            switch (metaDataParts[1])
            {
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

    public void Append(Classification classification, string body)
    {
        TodoItem item = new();
        item.Finished = false;
        item.Body = body;
        item.Classification = classification;

        for (int i = 0; i < _todoItems.Count; i++)
        {
            TodoItem current = _todoItems[i];
            if (current.Classification == Classification.Regular && classification == Classification.Important)
            {
                _todoItems.Insert(i, item);
                return;
            }
        }

        _todoItems.Add(item);
    }

    public bool Delete(int index)
    {
        if (index < 1 || index - 1 > _todoItems.Count)
        {
            return false;
        }

        _todoItems.RemoveAt(index - 1);
        return true;
    }

    public void DeleteAll()
    {
        Message.Debug($"Filename: {fileName}");
        Message.Debug($"Full path: {Path.GetFullPath(fileName)}");
        Message.Debug("DeleteAll");
        _todoItems.Clear();
    }

    public void WriteTodos()
    {
        int index = 1;

        List<TodoItem> regularTodoItem = new List<TodoItem>();

        foreach (TodoItem todoItem in _todoItems)
        {
            string value = todoItem.Finished ? "x" : " ";
            if (todoItem.Classification == Classification.Important)
            {
                Console.WriteLine($"{index++}.\t[{value}] {todoItem.Body}");
            }
            else
            {
                regularTodoItem.Add(todoItem);
            }
        }

        Console.WriteLine("");

        foreach (TodoItem todoItem in regularTodoItem)
        {
            string value = todoItem.Finished ? "x" : " ";
            Console.WriteLine($"{index++}.\t[{value}] {todoItem.Body}");
        }
    }

    public void Save()
    {
        List<string> rawLines = [];
        foreach (TodoItem current in _todoItems)
        {
            string finishedString = current.Finished ? "x" : "_";
            rawLines.Add($"{current.Classification} {finishedString}: {current.Body}");
        }

        File.WriteAllLines(fileName, rawLines);
    }

    public bool ToggleFinished(int doneIndex)
    {
        if (doneIndex < 1 || doneIndex - 1 > _todoItems.Count)
        {
            return false;
        }

        _todoItems[doneIndex - 1].Finished = !_todoItems[doneIndex - 1].Finished;
        return true;
    }
}