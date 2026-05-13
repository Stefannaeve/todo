namespace todo;

public class Todo(MyFile file) {
    public Classification Classification { get; set; } = Classification.Regular;
    public string body { get; set; } = "";

    public void Add() {
        file.Append(Classification, body);
    }

    public bool Delete(int index) {
        return file.Delete(index);
    }
    public bool Finish(int doneIndex) {
        return file.Finish(doneIndex);
    }
}
