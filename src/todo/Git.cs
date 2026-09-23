using System.ComponentModel;
using System.Diagnostics;
using todo.HelperClasses;

namespace todo;

public sealed class GitException(string message) : Exception(message);

public sealed record GitSyncTarget(string Remote, string Branch, string LocalBranch);

public class Git(string repoPath)
{
    private (int ExitCode, string Output) RunProcess(params string[] arguments)
    {
        using Process process = new();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = repoPath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        process.StartInfo.Environment["GCM_INTERACTIVE"] = "never";
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new GitException($"Could not start Git. Check that Git is installed and the task directory is accessible. {exception.Message}");
        }

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new GitException($"git {arguments[0]} timed out after 30 seconds. Check connectivity and credentials, or use --offline for local tasks.");
        }
        Task.WaitAll(output, error);

        if (Message.InfoEnabled || Message.VerboseEnabled)
        {
            Console.Write(output.Result);
        }

        if (process.ExitCode != 0 && !(arguments[0] == "diff" && process.ExitCode == 1))
        {
            string details = string.IsNullOrWhiteSpace(error.Result) ? output.Result.Trim() : error.Result.Trim();
            throw new GitException($"git {arguments[0]} failed (exit {process.ExitCode}): {details}");
        }

        return (process.ExitCode, output.Result);
    }

    public void EnsureInitialized()
    {
        Directory.CreateDirectory(repoPath);
        string gitPath = Path.Combine(repoPath, ".git");
        if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
        {
            Init();
        }
    }

    public void EnsureNoConflicts()
    {
        if (!string.IsNullOrWhiteSpace(RunProcess("ls-files", "--unmerged").Output))
        {
            throw new GitException("The task repository has unresolved conflicts. Resolve or abort the Git operation manually before using todo, including --offline. No task changes were saved.");
        }

        foreach (string marker in new[] { "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "rebase-merge", "rebase-apply", "sequencer" })
        {
            string path = Path.Combine(MetadataPath, marker);
            if (File.Exists(path) || Directory.Exists(path))
                throw new GitException("A Git operation is in progress. Complete or abort it manually before changing tasks.");
        }
    }

    public string MetadataPath
    {
        get
        {
            string path = Path.Combine(Path.GetFullPath(repoPath), ".git");
            if (!File.Exists(path)) return path;
            string pointer = File.ReadAllText(path).Trim();
            if (!pointer.StartsWith("gitdir: ", StringComparison.Ordinal))
                throw new GitException("Invalid .git worktree pointer.");
            return Path.GetFullPath(pointer[8..], Path.GetFullPath(repoPath));
        }
    }

    private bool HasRemote() => !string.IsNullOrWhiteSpace(RunProcess("remote").Output);

    private (string Remote, string Branch) TrackingTarget()
    {
        string head = RunProcess("symbolic-ref", "--quiet", "HEAD").Output.Trim();
        string[] target = RunProcess("for-each-ref", "--format=%(upstream:remotename)%00%(upstream:remoteref)", head).Output.Trim().Split('\0');
        if (target.Length != 2 || string.IsNullOrEmpty(target[0]) || string.IsNullOrEmpty(target[1]))
        {
            throw new GitException("The current branch has no upstream. Configure its remote tracking branch with Git before syncing, or use --offline for local tasks.");
        }
        return (target[0], target[1]);
    }

    public void Init() => RunProcess("init");

    public void Clone(string repoToClone) => RunProcess("clone", repoToClone);

    public void Push(string commitMessage)
    {
        EnsureNoConflicts();
        if (!HasRemote())
        {
            return;
        }

        (string Remote, string Branch) target = TrackingTarget();
        CommitPending(commitMessage);
        // Use the same upstream as Pull, regardless of push.default or pushRemote.
        RunProcess("push", "--", target.Remote, $"HEAD:{target.Branch}");
    }

    private static bool IsTaskPath(string path) => path == "todo.txt" || CompletionArchive.IsArchivePath(path);

    private string[] PendingPaths()
    {
        return RunProcess("ls-files", "--modified", "--deleted", "--others", "-z").Output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(IsTaskPath)
            .Concat(RunProcess("diff", "--cached", "--name-only", "-z").Output
                .Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(IsTaskPath))
            .Distinct(StringComparer.Ordinal).ToArray();
    }

    public bool HasPendingChanges() => PendingPaths().Length > 0;

    public void CommitPending(string message)
    {
        string[] paths = PendingPaths();
        if (paths.Length == 0) return;
        RunProcess(["add", "--", .. paths]);
        if (RunProcess(["diff", "--cached", "--quiet", "--", .. paths]).ExitCode == 1)
            RunProcess(["commit", "-m", message, "--", .. paths]);
    }

    public GitSyncTarget? PrepareSync()
    {
        EnsureNoConflicts();
        if (!HasRemote()) return null;
        (string Remote, string Branch) target = TrackingTarget();
        return new GitSyncTarget(target.Remote, target.Branch, RunProcess("symbolic-ref", "--quiet", "HEAD").Output.Trim());
    }

    public void EnsureTargetUnchanged(GitSyncTarget expected)
    {
        if (PrepareSync() != expected)
            throw new GitException("The Git branch or upstream changed during synchronization. Run todo sync again.");
    }

    public string Fetch(GitSyncTarget target)
    {
        RunProcess("fetch", "--", target.Remote, target.Branch);
        return RunProcess("rev-parse", "FETCH_HEAD").Output.Trim();
    }

    public void Integrate(string commit) => RunProcess("merge", "--ff-only", "--no-autostash", "--", commit);
    public string Head() => RunProcess("rev-parse", "HEAD").Output.Trim();
    public void PushCommit(GitSyncTarget target, string commit) => RunProcess("push", "--", target.Remote, $"{commit}:{target.Branch}");

    public void Pull()
    {
        EnsureNoConflicts();
        if (!HasRemote())
        {
            return;
        }

        (string Remote, string Branch) target = TrackingTarget();
        try
        {
            // Never create a merge or rebase implicitly, even if user config requests it.
            RunProcess("pull", "--ff-only", "--no-rebase", "--no-autostash", "--", target.Remote, target.Branch);
        }
        catch (GitException exception)
        {
            throw new GitException($"Synchronization stopped before applying the task command. {exception.Message}\nCheck connectivity and credentials; for diverged history or local changes, reconcile the repository manually. Use --offline only when you want to work on the local task list.");
        }
    }

    public void Status() => RunProcess("status");
}
