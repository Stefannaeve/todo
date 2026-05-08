namespace todo;

public class Todo(MyFile file)
{
    public Classification Classification { get; set; } = Classification.Regular;

    public bool All { get; set; }

    public void Add(string body)
    {
        file.Append(Classification, body);
    }

    public void Delete()
    {
        if (All)
        {
            file.DeleteAll();
        }
    }
}
