using System.Diagnostics;
using todo;

namespace Todo.Unit.Tests;

public sealed class GitSyncTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "todo-sync-" + Guid.NewGuid());
    private string Remote => Path.Combine(_root, "remote.git");
    private string Local => Path.Combine(_root, "local");
    private string Other => Path.Combine(_root, "other");

    public GitSyncTests()
    {
        Directory.CreateDirectory(_root);
        Run(_root, "init", "--bare", "--initial-branch=main", Remote);
        Run(_root, "clone", Remote, Local);
        Configure(Local);
        File.WriteAllText(Path.Combine(Local, "README"), "Tasks repository");
        Run(Local, "add", "README");
        Run(Local, "commit", "-m", "Initial");
        Run(Local, "push", "-u", "origin", "main");
        Run(_root, "clone", Remote, Other);
        Configure(Other);
    }

    [Fact]
    public void PushTracksNewTodoAndLeavesUnrelatedStagedFilesAlone()
    {
        File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: First task\n");
        File.WriteAllText(Path.Combine(Local, "unrelated.txt"), "Keep staged");
        Run(Local, "add", "unrelated.txt");
        Git git = new(Local);
        git.Pull();
        git.Push("Add task");

        Assert.Contains("First task", Run(Remote, "show", "main:todo.txt"));
        Assert.Equal("todo.txt", Run(Local, "diff-tree", "--no-commit-id", "--name-only", "-r", "HEAD").Trim());
        Assert.Equal("unrelated.txt", Run(Local, "diff", "--cached", "--name-only").Trim());
        git.Push("No changes");
    }

    [Fact]
    public void FailedPullLeavesTasksUnchanged()
    {
        string task = Path.Combine(Local, "todo.txt");
        File.WriteAllText(task, "Regular _: Keep me\n");
        Run(Local, "remote", "set-url", "origin", Path.Combine(_root, "missing.git"));

        GitException error = Assert.Throws<GitException>(() => new Git(Local).Pull());

        Assert.Contains("before applying", error.Message);
        Assert.Equal("Regular _: Keep me\n", File.ReadAllText(task));
    }

    [Fact]
    public void DivergenceDoesNotCreateMergeOrRebase()
    {
        Commit(Local, "local.txt", "Local", "Local change");
        Commit(Other, "remote.txt", "Remote", "Remote change");
        Run(Other, "push");
        Run(Local, "config", "pull.rebase", "true");
        string head = Run(Local, "rev-parse", "HEAD");

        Assert.Throws<GitException>(() => new Git(Local).Pull());

        Assert.Equal(head, Run(Local, "rev-parse", "HEAD"));
        Assert.False(File.Exists(Path.Combine(Local, ".git", "MERGE_HEAD")));
        Assert.False(Directory.Exists(Path.Combine(Local, ".git", "rebase-merge")));
        Assert.Equal("Local", File.ReadAllText(Path.Combine(Local, "local.txt")));
    }

    [Fact]
    public void ConflictsAndPendingResolutionsBlockTaskOperations()
    {
        Commit(Local, "todo.txt", "Regular _: Base\n", "Base");
        Run(Local, "push");
        Run(Other, "pull", "--ff-only");
        Commit(Local, "todo.txt", "Regular _: Local\n", "Local");
        Commit(Other, "todo.txt", "Regular _: Remote\n", "Remote");
        Run(Other, "push");
        Run(Local, "fetch");
        Assert.Throws<InvalidOperationException>(() => Run(Local, "merge", "origin/main"));
        string conflict = File.ReadAllText(Path.Combine(Local, "todo.txt"));
        Git git = new(Local);

        Assert.Throws<GitException>(git.EnsureNoConflicts);
        Assert.Throws<GitException>(git.Pull);
        Assert.Throws<GitException>(() => git.Push("Must not commit"));
        Assert.Equal(conflict, File.ReadAllText(Path.Combine(Local, "todo.txt")));

        File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Resolution\n");
        Run(Local, "add", "todo.txt");
        Assert.Throws<GitException>(git.EnsureNoConflicts);
    }

    [Fact]
    public void RejectedPushKeepsLocalTaskAndCommit()
    {
        Git git = new(Local);
        git.Pull();
        Commit(Other, "remote.txt", "Advance upstream", "Remote change");
        Run(Other, "push");
        File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Saved locally\n");

        Assert.Throws<GitException>(() => git.Push("Local task"));

        Assert.Equal("Local task", Run(Local, "log", "-1", "--format=%s").Trim());
        Assert.Contains("Saved locally", Run(Local, "show", "HEAD:todo.txt"));
        Assert.Equal("Remote change", Run(Remote, "log", "-1", "--format=%s").Trim());
    }

    [Fact]
    public void MissingUpstreamProducesSetupGuidance()
    {
        Run(Local, "branch", "--unset-upstream");
        Assert.Contains("no upstream", Assert.Throws<GitException>(() => new Git(Local).Pull()).Message);
    }

    [Theory]
    [InlineData("add", "Task")]
    [InlineData("delete", "1")]
    [InlineData("done", "1")]
    [InlineData("list", null)]
    public void OfflineFlagIsAccepted(string command, string? value)
    {
        string[] args = value is null ? [command, "--offline"] : [command, value, "--offline"];
        Assert.Contains(ArgumentParser.parseArgs(args).Arguments, argument => argument.ArgumentType == ArgumentType.Offline);
    }

    private static void Configure(string path)
    {
        Run(path, "config", "user.name", "Todo test");
        Run(path, "config", "user.email", "todo@example.invalid");
        Run(path, "config", "commit.gpgsign", "false");
        Run(path, "config", "core.hooksPath", Path.Combine(path, "empty-hooks"));
    }

    private static void Commit(string path, string file, string content, string message)
    {
        File.WriteAllText(Path.Combine(path, file), content);
        Run(path, "add", "--", file);
        Run(path, "commit", "-m", message);
    }

    private static string Run(string path, params string[] args)
    {
        using Process process = new();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = path;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        foreach (string arg in args) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(stdout, stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException(stderr.Result);
        return stdout.Result;
    }

    public void Dispose()
    {
        // Git object files may be read-only on Windows.
        foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_root, recursive: true);
    }
}
