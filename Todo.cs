namespace todo;

public class Todo(MyFile file) {
    public Classification Classification { get; set; } = Classification.Regular;
    public string body { get; set; } = "";
    public int number { get; set; } = 0;

    public bool All { get; set; }

    public void Add() {
        file.Append(Classification, body);
    }

    public void Delete() {
        if (All) {
            file.DeleteAll();
            Environment.Exit(0);
        }
        file.Delete(number);
    }
}