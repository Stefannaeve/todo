using todo;

namespace Todo.Unit.Tests;

public sealed class CompletionUndoTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todo-undo-" + Guid.NewGuid());
    private readonly DateOnly _date = new(2026, 1, 23);
    private string TaskPath => Path.Combine(_root, "todo.txt");
    private string ArchivePath => Path.Combine(_root, "2026/january/23-01");
    private CompletionUndo UndoStore => new(_root, Path.Combine(_root, ".git"));

    public CompletionUndoTests() => Directory.CreateDirectory(_root);

    private MyFile Read()
    {
        MyFile file = new(TaskPath);
        file.ParseFile();
        return file;
    }

    [Fact]
    public void UndoSurvivesNewInstanceAndPreservesInterveningTasksAndPriority()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Important, "Finish: café ☕");
        file.Append(Classification.Regular, "Other task");
        file.Save();
        UndoEntry done = UndoStore.Complete(file, 1, _date);
        Assert.Equal("Finish: café ☕", done.Item.Body);
        MyFile later = Read();
        later.Append(Classification.Regular, "Added later");
        later.Save();

        UndoEntry restored = UndoStore.Undo(Read());

        Assert.Equal(done.Item.Body, restored.Item.Body);
        Assert.Equal(["Important _: Finish: café ☕", "Regular _: Other task", "Regular _: Added later"], File.ReadAllLines(TaskPath));
        Assert.Empty(File.ReadAllBytes(ArchivePath));
        Assert.Throws<InvalidDataException>(() => UndoStore.Undo(Read()));
        Assert.Equal(3, Read().Count);
    }

    [Fact]
    public void OnlyLastDoneCanBeUndoneEvenForIdenticalTaskText()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Same task");
        file.Append(Classification.Regular, "Same task");
        file.Save();
        UndoStore.Complete(file, 1, _date);
        UndoStore.Complete(file, 1, _date);

        UndoStore.Undo(Read());

        Assert.Equal("Regular _: Same task", File.ReadAllLines(TaskPath).Single());
        Assert.Equal("Regular x: Same task", File.ReadAllLines(ArchivePath).Single());
        Assert.Throws<InvalidDataException>(() => UndoStore.Validate());
    }

    [Fact]
    public void UndoRestoresExactPreviousArchiveWithoutNewline()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ArchivePath)!);
        File.WriteAllText(ArchivePath, "Regular x: Earlier");
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "New completion");
        file.Save();
        UndoStore.Complete(file, 1, _date);

        UndoStore.Undo(Read());

        Assert.Equal("Regular x: Earlier", File.ReadAllText(ArchivePath));
        Assert.Equal("Regular _: New completion", File.ReadAllLines(TaskPath).Single());
    }

    [Fact]
    public void ChangedArchiveRefusesUndoWithoutChangingFiles()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Task");
        file.Save();
        UndoStore.Complete(file, 1, _date);
        File.AppendAllText(ArchivePath, "Regular x: From another computer\n");
        byte[] before = File.ReadAllBytes(ArchivePath);

        Assert.Throws<InvalidDataException>(() => UndoStore.Undo(Read()));

        Assert.Empty(File.ReadAllBytes(TaskPath));
        Assert.Equal(before, File.ReadAllBytes(ArchivePath));
    }

    [Fact]
    public void FailedCompletionKeepsPreviousUndoRecord()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "First");
        file.Append(Classification.Regular, "Second");
        file.Save();
        UndoStore.Complete(file, 1, _date);
        File.WriteAllText(Path.Combine(_root, "2027"), "Blocks next archive");

        Assert.ThrowsAny<IOException>(() => UndoStore.Complete(file, 1, new DateOnly(2027, 1, 1)));
        UndoStore.Undo(Read());

        Assert.Equal(["Regular _: First", "Regular _: Second"], File.ReadAllLines(TaskPath));
    }

    [Fact]
    public void FailedUndoRecordWriteRollsBackCompletion()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        File.WriteAllText(Path.Combine(_root, ".git", "todo-sync"), "Blocks undo metadata");
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Keep active");
        file.Save();

        Assert.ThrowsAny<IOException>(() => UndoStore.Complete(file, 1, _date));

        Assert.Equal("Regular _: Keep active", File.ReadAllLines(TaskPath).Single());
        Assert.False(File.Exists(ArchivePath));
    }

    [Fact]
    public void UndoDeleteRestoresOriginalPriorityStatusAndPreservesNewTasks()
    {
        File.WriteAllLines(TaskPath, ["Important x: Deleted task", "Regular _: Other"]);
        MyFile file = Read();
        UndoStore.Delete(file, 1, all: false);
        file.Append(Classification.Regular, "Added later");
        file.Save();

        UndoStore.Undo(Read());

        Assert.Equal(["Important x: Deleted task", "Regular _: Other", "Regular _: Added later"], File.ReadAllLines(TaskPath));
        Assert.False(File.Exists(ArchivePath));
        Assert.Throws<InvalidDataException>(() => UndoStore.Validate());
    }

    [Fact]
    public void UndoDeleteAllRestoresDuplicateTasksAndPreservesNewEntries()
    {
        File.WriteAllLines(TaskPath, ["Important _: First", "Regular _: Same", "Regular _: Same"]);
        MyFile file = Read();
        UndoStore.Delete(file, 0, all: true);
        Assert.Empty(File.ReadAllBytes(TaskPath));
        Assert.Null(UndoStore.Delete(file, 0, all: true));
        file.Append(Classification.Regular, "New task");
        file.Save();

        UndoStore.Undo(Read());

        Assert.Equal(["Important _: First", "Regular _: Same", "Regular _: Same", "Regular _: New task"], File.ReadAllLines(TaskPath));
        Assert.Throws<InvalidDataException>(() => UndoStore.Validate());
    }

    [Fact]
    public void DeleteReplacesRememberedDoneWithoutChangingArchive()
    {
        File.WriteAllLines(TaskPath, ["Regular _: Complete me", "Regular _: Delete me"]);
        MyFile file = Read();
        UndoStore.Complete(file, 1, _date);
        UndoStore.Delete(file, 1, all: false);
        UndoStore.Undo(Read());

        Assert.Equal("Regular _: Delete me", File.ReadAllLines(TaskPath).Single());
        Assert.Equal("Regular x: Complete me", File.ReadAllLines(ArchivePath).Single());
        Assert.Throws<InvalidDataException>(() => UndoStore.Validate());
    }

    [Fact]
    public void DoneReplacesRememberedDelete()
    {
        File.WriteAllLines(TaskPath, ["Regular _: Delete me", "Regular _: Complete me"]);
        MyFile file = Read();
        UndoStore.Delete(file, 1, all: false);
        UndoStore.Complete(file, 1, _date);
        UndoStore.Undo(Read());
        Assert.Equal("Regular _: Complete me", File.ReadAllLines(TaskPath).Single());
        Assert.Throws<InvalidDataException>(() => UndoStore.Validate());
    }

    [Fact]
    public void FailedDeletionRecordWriteRestoresActiveFile()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        File.WriteAllText(Path.Combine(_root, ".git", "todo-sync"), "Blocks metadata");
        File.WriteAllText(TaskPath, "Regular _: Keep me\n");
        Assert.ThrowsAny<IOException>(() => UndoStore.Delete(Read(), 1, all: false));
        Assert.Equal("Regular _: Keep me\n", File.ReadAllText(TaskPath));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
