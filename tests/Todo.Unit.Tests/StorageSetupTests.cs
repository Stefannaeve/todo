using todo;

namespace Todo.Unit.Tests;

public sealed class StorageSetupTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todo-setup-" + Guid.NewGuid());

    [Fact]
    public void FreshStorageSupportsLocalTasksWithoutRemote()
    {
        string path = Path.Combine(_root, "nested", "task storage");
        Git git = new(path);
        git.EnsureInitialized();
        Assert.True(Directory.Exists(Path.Combine(path, ".git")));
        git.Pull();

        MyFile file = new(Path.Combine(path, "todo.txt"));
        file.ParseFile();
        file.Append(Classification.Regular, "My first task");
        file.Save();
        git.Push("Added new Todo");

        Assert.Equal("Regular _: My first task", File.ReadAllLines(Path.Combine(path, "todo.txt")).Single());
    }

    [Fact]
    public void RepeatedSetupPreservesExistingTasksAndRepositoryConfiguration()
    {
        Git git = new(_root);
        git.EnsureInitialized();
        string configPath = Path.Combine(_root, ".git", "config");
        File.AppendAllText(configPath, "\n[remote \"origin\"]\n\turl = https://example.invalid/tasks.git\n");
        string config = File.ReadAllText(configPath);
        string todoPath = Path.Combine(_root, "todo.txt");
        File.WriteAllText(todoPath, "Important _: Keep this task");

        git.EnsureInitialized();

        Assert.Equal(config, File.ReadAllText(configPath));
        Assert.Equal("Important _: Keep this task", File.ReadAllText(todoPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
