using System.Diagnostics;
using todo.HelperClasses;

namespace todo;

public class Git(string repoPath)
{
    private (int ExitCode, string Output) RunProcess(params string[] arguments)
    {
        using Process process = new();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = repoPath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        Task.WaitAll(output, error);

        if (Message.InfoEnabled || Message.VerboseEnabled)
        {
            Console.Write(output.Result);
        }

        if (process.ExitCode != 0 && !(arguments[0] == "diff" && process.ExitCode == 1))
        {
            throw new InvalidOperationException($"git {arguments[0]} failed: {error.Result.Trim()}");
        }

        return (process.ExitCode, output.Result);
    }

    public void EnsureInitialized()
    {
        Directory.CreateDirectory(repoPath);
        string gitPath = Path.Combine(repoPath, ".git");
        // Worktrees use a .git file; regular repositories use a directory.
        if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
        {
            Init();
        }
    }

    private bool HasRemote() => !string.IsNullOrWhiteSpace(RunProcess("remote").Output);

    public void Init() => RunProcess("init");

    public void Clone(string repoToClone) => RunProcess("clone", repoToClone);

    public void Push(string commitMessage)
    {
        // Fresh installations work locally without a Git identity or remote.
        if (!HasRemote())
        {
            return;
        }

        RunProcess("add", "--", "todo.txt");
        if (RunProcess("diff", "--cached", "--quiet", "--", "todo.txt").ExitCode == 1)
        {
            RunProcess("commit", "-m", commitMessage, "--", "todo.txt");
        }
        RunProcess("push");
    }

    public void Pull()
    {
        if (HasRemote())
        {
            RunProcess("pull");
        }
    }

    public void Status() => RunProcess("status");
}
