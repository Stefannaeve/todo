using System.Diagnostics;
using System.Text;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

public class MyFile(string fileName)
{
    private List<TodoItem> _todoItems = [];
    private readonly Parser _parser = new();
    public int Count => _todoItems.Count;
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
        if (index < 1 || index > _todoItems.Count)
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
        string fullPath = Path.GetFullPath(fileName);
        string temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, $".todo-{Guid.NewGuid():N}.tmp");
        try
        {
            // Replace only after the complete new file has been written.
            File.WriteAllLines(temporary, _todoItems.Select(current =>
                $"{current.Classification} {(current.Finished ? "x" : "_")}: {current.Body}"));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    public string? Complete(int index, DateOnly date)
    {
        if (index < 1 || index > _todoItems.Count)
        {
            return null;
        }
        if (ParseErrors.Count > 0)
        {
            throw new InvalidDataException("Cannot archive tasks while todo.txt contains invalid lines. Fix those lines first; no tasks were changed.");
        }

        string relativePath = CompletionArchive.RelativePath(date);
        string archivePath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(fileName))!, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        bool existed = File.Exists(archivePath);
        bool completed = false;
        try
        {
            using FileStream archive = new(archivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            long originalLength = archive.Length;
            TodoItem item = _todoItems[index - 1];
            bool removed = false;
            try
            {
                bool needsNewline = false;
                if (originalLength > 0)
                {
                    archive.Position = originalLength - 1;
                    int lastByte = archive.ReadByte();
                    needsNewline = lastByte != '\n' && lastByte != '\r';
                }
                archive.Position = originalLength;
                using (StreamWriter writer = new(archive, new UTF8Encoding(false), leaveOpen: true))
                {
                    if (needsNewline) writer.WriteLine();
                    writer.WriteLine($"{item.Classification} x: {item.Body}");
                }
                // Preserve the completed task before removing it from the active file.
                archive.Flush(flushToDisk: true);
                _todoItems.RemoveAt(index - 1);
                removed = true;
                Save();
                completed = true;
                return relativePath;
            }
            catch
            {
                if (removed) _todoItems.Insert(index - 1, item);
                // A failed active-file save must not leave a second archived copy.
                archive.SetLength(originalLength);
                archive.Flush(flushToDisk: true);
                throw;
            }
        }
        finally
        {
            if (!completed && !existed && File.Exists(archivePath) && new FileInfo(archivePath).Length == 0)
            {
                File.Delete(archivePath);
            }
        }
    }
}
