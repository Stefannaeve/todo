using System.Diagnostics;

namespace todo;



public class Todo {
    
    private int Count { get; set; }
    
    private int index;
    public Classification classification { get; set; }
    private string body;

    public bool all { get; set; }
    
    private MyFile _file;

    public Todo(MyFile file) {
        _file = file;
        classification = Classification.Regular;
        all = false;
    }

    public void add(string body) {
        _file.Append(classification, body);
    }

    public void delete() {
        if (all) {
            _file.deleteAll();
        }

    }
}