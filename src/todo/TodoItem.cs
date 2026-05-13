namespace todo;

public class TodoItem {
    public Classification Classification { get; set; }
    public bool Finished { get; set; }
    public string Body { get; set; } = "";
}