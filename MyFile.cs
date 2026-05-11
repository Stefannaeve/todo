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
        HelperClasses.Message.Debug($"Filename: {_fileName}");
        HelperClasses.Message.Debug($"Full path: {Path.GetFullPath(_fileName)}");
        HelperClasses.Message.Debug("DeleteAll");
        File.WriteAllText(_fileName, string.Empty);
    }
}