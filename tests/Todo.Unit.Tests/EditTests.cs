using todo;

namespace Todo.Unit.Tests;

public sealed class EditTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todo-edit-" + Guid.NewGuid());
    private string TaskPath => Path.Combine(_root, "todo.txt");

    public EditTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void EditPreservesPriorityOrderAndCompletionFlag()
    {
        File.WriteAllLines(TaskPath, ["Important _: First", "Important x: Second", "Regular _: Third"]);
        MyFile file = new(TaskPath);
        file.ParseFile();
        Assert.True(file.Edit(2, "Updated: café ☕"));
        file.Save();
        Assert.Equal(["Important _: First", "Important x: Updated: café ☕", "Regular _: Third"], File.ReadAllLines(TaskPath));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void InvalidIndexLeavesTaskUnchanged(int index)
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Original");
        Assert.False(file.Edit(index, "Replacement"));
        file.Save();
        Assert.Equal("Regular _: Original", File.ReadAllLines(TaskPath).Single());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("First\nSecond")]
    [InlineData("First\rSecond")]
    public void InvalidTextCannotCorruptFile(string text)
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Original");
        Assert.Throws<ArgumentException>(() => file.Edit(1, text));
        file.Save();
        Assert.Equal("Regular _: Original", File.ReadAllLines(TaskPath).Single());
    }

    [Fact]
    public void EditDoesNotReplaceLastCompletionUndo()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Completed task");
        file.Append(Classification.Regular, "Active task");
        file.Save();
        CompletionUndo undo = new(_root, Path.Combine(_root, ".git"));
        undo.Complete(file, 1, new DateOnly(2026, 1, 23));
        Assert.True(file.Edit(1, "Edited active task"));
        file.Save();

        undo.Undo(file);

        Assert.Equal(["Regular _: Completed task", "Regular _: Edited active task"], File.ReadAllLines(TaskPath));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
