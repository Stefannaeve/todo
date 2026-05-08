using System.IO;
using System.IO.Enumeration;
using System.Text;

namespace todo;

public class MyFile {
    private string _fileName;
    private List<string> _lines;

    public MyFile(string fileName) {
        _fileName = fileName;
        if (!File.Exists(_fileName)) {
            File.Create(_fileName);
        }
        _lines = File.ReadAllLines(_fileName).ToList();
    }

    public void Append(Classification classification, string body) {
        int count = _lines.Count + 1;
        StringBuilder stringBuilder = new StringBuilder("[");
        stringBuilder.Append(count);
        stringBuilder.Append("] ");
        stringBuilder.Append("[");
        stringBuilder.Append(classification.ToString());
        stringBuilder.Append("]: ");
        stringBuilder.Append(body);
        _lines.Add(stringBuilder.ToString());
        File.WriteAllLines(_fileName, _lines);
    }

    public void deleteAll() {
        File.WriteAllText(_fileName, string.Empty);
    }
}