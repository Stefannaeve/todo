namespace todo;

public class MyFile {
    private readonly string _fileName;
    private readonly List<string> _lines;

    public MyFile(string fileName) {
        _fileName = fileName;
        if (!File.Exists(_fileName)) {
            File.Create(_fileName);
        }

        _lines = File.ReadAllLines(_fileName).ToList();
    }

    public void Append(Classification classification, string body) {
        _lines.Add($"[{_lines.Count + 1}] [{classification}] {body}");
        File.WriteAllLines(_fileName, _lines);
    }

    public void DeleteAll() {
        Console.WriteLine($"Filename: {_fileName}");
        Console.WriteLine($"Full path: {Path.GetFullPath(_fileName)}");
        Console.WriteLine("DeleteAll");
        File.WriteAllText(_fileName, string.Empty);
    }
}