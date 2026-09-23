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

    [Fact]
    public void PushIncludesOfflineArchivesWithTheActiveList()
    {
        string taskPath = Path.Combine(Local, "todo.txt");
        MyFile file = new(taskPath);
        file.Append(Classification.Important, "First completion");
        file.Append(Classification.Regular, "Next day");
        file.Save();
        Assert.NotNull(file.Complete(1, new DateOnly(2026, 1, 31)));
        Assert.NotNull(file.Complete(1, new DateOnly(2026, 2, 1)));
        File.WriteAllText(Path.Combine(Local, "unrelated.txt"), "Keep staged");
        Run(Local, "add", "unrelated.txt");

        new Git(Local).Push("Synchronize completions");

        Assert.Empty(Run(Remote, "show", "main:todo.txt"));
        Assert.Contains("Important x: First completion", Run(Remote, "show", "main:2026/january/31-01"));
        Assert.Contains("Regular x: Next day", Run(Remote, "show", "main:2026/february/01-02"));
        Assert.Equal("unrelated.txt", Run(Local, "diff", "--cached", "--name-only").Trim());
        Assert.Equal(3, Run(Local, "diff-tree", "--no-commit-id", "--name-only", "-r", "HEAD").Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void IgnoredArchiveStopsSynchronizationInsteadOfDroppingHistory()
    {
        string taskPath = Path.Combine(Local, "todo.txt");
        MyFile file = new(taskPath);
        file.Append(Classification.Regular, "Keep archived task");
        file.Save();
        Assert.NotNull(file.Complete(1, new DateOnly(2026, 1, 23)));
        File.AppendAllText(Path.Combine(Local, ".git", "info", "exclude"), "\n2026/\n");

        Assert.Throws<GitException>(() => new Git(Local).Push("Must fail"));

        Assert.Equal("Initial", Run(Remote, "log", "-1", "--format=%s").Trim());
        Assert.Contains("Keep archived task", File.ReadAllText(Path.Combine(Local, "2026/january/23-01")));
    }

    [Fact]
    public void CoordinatorSynchronizesLocalDataAndRecordsSuccess()
    {
        SyncCoordinator sync = new(Local);
        using (FileStream fileLock = sync.LockFiles())
        {
            sync.MarkPending(requestSync: true);
            File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Queued task\n");
        }
        Assert.True(sync.Synchronize(background: false));
        SyncState state = sync.ReadState();
        Assert.Equal(state.Revision, state.SyncedRevision);
        Assert.Equal(state.Request, state.CompletedRequest);
        Assert.NotNull(state.LastSuccess);
        Assert.Null(state.LastError);
        Assert.Contains("Queued task", Run(Remote, "show", "main:todo.txt"));
    }

    [Fact]
    public void ManualSyncFastForwardsRemoteTasksWithoutLocalEdits()
    {
        Commit(Other, "todo.txt", "Regular _: Remote task\n", "Remote task");
        Run(Other, "push");
        SyncCoordinator sync = new(Local);
        long request = sync.RequestSync();

        Assert.True(sync.Synchronize(background: false, requested: request));

        Assert.Equal("Regular _: Remote task\n", File.ReadAllText(Path.Combine(Local, "todo.txt")));
        Assert.Null(sync.ReadState().LastError);
    }

    [Fact]
    public void CoordinatorRetainsRequestsAndDataAfterFailureThenRetries()
    {
        SyncCoordinator sync = new(Local);
        using (FileStream fileLock = sync.LockFiles())
        {
            sync.MarkPending(requestSync: true);
            File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Durable task\n");
        }
        Run(Local, "remote", "set-url", "origin", Path.Combine(_root, "unavailable.git"));

        Assert.False(sync.Synchronize(background: true));
        SyncState failed = new SyncCoordinator(Local).ReadState();
        Assert.NotNull(failed.LastError);
        Assert.True(failed.Request > failed.CompletedRequest);
        Assert.Contains("Durable task", Run(Local, "show", "HEAD:todo.txt"));

        Run(Local, "remote", "set-url", "origin", Remote);
        long request = sync.RequestSync();
        Assert.True(sync.Synchronize(background: false, requested: request));
        Assert.Null(sync.ReadState().LastError);
        Assert.Contains("Durable task", Run(Remote, "show", "main:todo.txt"));
    }

    [Fact]
    public void CoordinatorPreservesDivergedHistoriesAndReportsFailure()
    {
        File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Local task\n");
        Commit(Other, "todo.txt", "Regular _: Remote task\n", "Remote task");
        Run(Other, "push");
        SyncCoordinator sync = new(Local);
        sync.RequestSync();

        Assert.False(sync.Synchronize(background: true));

        Assert.Equal("Regular _: Local task\n", File.ReadAllText(Path.Combine(Local, "todo.txt")));
        Assert.Equal("Regular _: Remote task", Run(Remote, "show", "main:todo.txt").Trim());
        Assert.NotNull(sync.ReadState().LastError);
        Assert.False(File.Exists(Path.Combine(Local, ".git", "MERGE_HEAD")));
    }

    [Fact]
    public void OfflineMarkerDoesNotRequestAWorkerButExplicitSyncIncludesIt()
    {
        SyncCoordinator sync = new(Local);
        using (FileStream fileLock = sync.LockFiles())
        {
            sync.MarkPending(requestSync: false);
            File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Offline task\n");
        }
        Assert.Equal(0, sync.ReadState().Request);
        Assert.Equal(1, sync.ReadState().Revision);
        Assert.False(sync.IsRunning());
        long request = sync.RequestSync();
        Assert.True(sync.Synchronize(background: false, requested: request));
        Assert.Contains("Offline task", Run(Remote, "show", "main:todo.txt"));
    }

    [Fact]
    public void SeparateWorkerSurvivesClosedConsoleAndSynchronizesPendingRequest()
    {
        SyncCoordinator sync = new(Local);
        using (FileStream fileLock = sync.LockFiles())
        {
            sync.MarkPending(requestSync: true);
            File.WriteAllText(Path.Combine(Local, "todo.txt"), "Regular _: Background task\n");
        }
        using Process worker = new();
        worker.StartInfo.FileName = "dotnet";
        worker.StartInfo.ArgumentList.Add(typeof(Git).Assembly.Location);
        worker.StartInfo.ArgumentList.Add("--sync-worker");
        worker.StartInfo.ArgumentList.Add(Local);
        worker.StartInfo.RedirectStandardInput = true;
        worker.StartInfo.RedirectStandardOutput = true;
        worker.StartInfo.RedirectStandardError = true;
        worker.Start();
        worker.StandardInput.Close();
        worker.StandardOutput.Close();
        worker.StandardError.Close();
        if (!worker.WaitForExit(15_000))
        {
            worker.Kill(entireProcessTree: true);
            worker.WaitForExit();
            Assert.Fail("Background worker did not finish.");
        }
        Assert.Equal(0, worker.ExitCode);
        Assert.NotNull(sync.ReadState().LastSuccess);
        Assert.Equal(sync.ReadState().Request, sync.ReadState().CompletedRequest);
        Assert.Contains("Background task", Run(Remote, "show", "main:todo.txt"));
        Assert.False(sync.IsRunning());
    }

    [Theory]
    [InlineData("add", "Task")]
    [InlineData("delete", "1")]
    [InlineData("done", "1")]
    [InlineData("list", null)]
    public void OfflineFlagIsAccepted(string command, string? value)
    {
        string[] args = value is null ? [command, "--offline"] : [command, "--offline", value];
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
