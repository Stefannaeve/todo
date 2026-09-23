using System.Globalization;
using todo;

namespace Todo.Unit.Tests;

public sealed class CompletionArchiveTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todo-archive-" + Guid.NewGuid());
    private string TaskPath => Path.Combine(_root, "todo.txt");

    public CompletionArchiveTests() => Directory.CreateDirectory(_root);

    [Theory]
    [InlineData(2026, 1, 23, "2026/january/23-01")]
    [InlineData(2028, 2, 29, "2028/february/29-02")]
    [InlineData(2026, 12, 31, "2026/december/31-12")]
    [InlineData(2027, 1, 1, "2027/january/01-01")]
    public void PathsUseEnglishMonthAndPaddedDayMonth(int year, int month, int day, string expected)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");
            Assert.Equal(expected, CompletionArchive.RelativePath(new DateOnly(year, month, day)));
            Assert.True(CompletionArchive.IsArchivePath(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [InlineData("2026/january/31-02")]
    [InlineData("2026/february/29-02")]
    [InlineData("2026/january/23-01.txt")]
    [InlineData("2026/January/23-01")]
    [InlineData("notes/2026/january/23-01")]
    public void UnrelatedPathsAreNotArchives(string path) => Assert.False(CompletionArchive.IsArchivePath(path));

    [Fact]
    public void CompletionRemovesTaskAndAppendsToExistingDayWithoutOverwriting()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Keep active");
        file.Append(Classification.Important, "Finish: café ☕");
        file.Save();
        string archive = Path.Combine(_root, "2026/january/23-01");
        Directory.CreateDirectory(Path.GetDirectoryName(archive)!);
        File.WriteAllText(archive, "Regular x: Earlier task");

        Assert.Equal("2026/january/23-01", file.Complete(1, new DateOnly(2026, 1, 23)));

        Assert.Equal("Regular _: Keep active", File.ReadAllLines(TaskPath).Single());
        Assert.Equal(["Regular x: Earlier task", "Important x: Finish: café ☕"], File.ReadAllLines(archive));
        Assert.NotNull(file.Complete(1, new DateOnly(2026, 1, 23)));
        Assert.Empty(File.ReadAllLines(TaskPath));
        Assert.Equal(3, File.ReadAllLines(archive).Length);
        Assert.Null(file.Complete(1, new DateOnly(2026, 1, 23)));
        Assert.Equal(3, File.ReadAllLines(archive).Length);
    }

    [Fact]
    public void ArchiveFailureLeavesActiveTaskUntouched()
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Keep me");
        file.Save();
        string before = File.ReadAllText(TaskPath);
        File.WriteAllText(Path.Combine(_root, "2026"), "Blocks the year directory");

        Assert.ThrowsAny<IOException>(() => file.Complete(1, new DateOnly(2026, 1, 23)));

        Assert.Equal(before, File.ReadAllText(TaskPath));
        file.Save();
        Assert.Equal(before, File.ReadAllText(TaskPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActiveSaveFailureRollsBackArchiveAndInMemoryRemoval(bool existingArchive)
    {
        MyFile file = new(TaskPath);
        file.Append(Classification.Regular, "Keep me");
        file.Save();
        string before = File.ReadAllText(TaskPath);
        string archive = Path.Combine(_root, "2026/january/23-01");
        if (existingArchive)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(archive)!);
            File.WriteAllText(archive, "Regular x: Earlier task");
        }
        File.Move(TaskPath, TaskPath + ".backup");
        Directory.CreateDirectory(TaskPath);

        Assert.ThrowsAny<IOException>(() => file.Complete(1, new DateOnly(2026, 1, 23)));

        Assert.Equal(before, File.ReadAllText(TaskPath + ".backup"));
        if (existingArchive) Assert.Equal("Regular x: Earlier task", File.ReadAllText(archive));
        else Assert.False(File.Exists(archive));
        Directory.Delete(TaskPath);
        file.Save();
        Assert.Equal(before, File.ReadAllText(TaskPath));
    }

    [Fact]
    public void MalformedActiveFileBlocksCompletion()
    {
        string before = "Regular _: Keep me\nInvalid task line\n";
        File.WriteAllText(TaskPath, before);
        MyFile file = new(TaskPath);
        file.ParseFile();

        Assert.Throws<InvalidDataException>(() => file.Complete(1, new DateOnly(2026, 1, 23)));

        Assert.Equal(before, File.ReadAllText(TaskPath));
        Assert.False(Directory.Exists(Path.Combine(_root, "2026")));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
