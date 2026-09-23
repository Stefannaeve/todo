using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace todo;

public sealed class SyncState
{
    public long Revision { get; set; }
    public long SyncedRevision { get; set; }
    public long Request { get; set; }
    public long CompletedRequest { get; set; }
    public DateTimeOffset? LastSuccess { get; set; }
    public string? LastError { get; set; }
    public string? Note { get; set; }
}

public sealed class SyncCoordinator
{
    private readonly string _repoPath;
    private readonly string _directory;
    private readonly Git _git;

    public SyncCoordinator(string repoPath)
    {
        _repoPath = Path.GetFullPath(repoPath);
        _git = new Git(_repoPath);
        _directory = Path.Combine(_git.MetadataPath, "todo-sync");
        Directory.CreateDirectory(_directory);
    }

    public FileStream LockFiles() => Acquire(Path.Combine(_directory, "files.lock"), 5_000);

    private static FileStream Acquire(string path, int timeout)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException) when (elapsed.ElapsedMilliseconds < timeout)
            {
                Thread.Sleep(20);
            }
        }
    }

    public SyncState ReadState()
    {
        string path = Path.Combine(_directory, "state.json");
        return File.Exists(path) ? JsonSerializer.Deserialize<SyncState>(File.ReadAllText(path)) ?? new() : new();
    }

    private void WriteState(SyncState state)
    {
        string path = Path.Combine(_directory, "state.json");
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state));
        File.Move(temporary, path, overwrite: true);
    }

    // Caller holds the file lock, before changing task files. A crash leaves a durable request.
    public void MarkPending(bool requestSync)
    {
        SyncState state = ReadState();
        state.Revision++;
        if (requestSync) state.Request++;
        WriteState(state);
    }

    public long RequestSync()
    {
        using FileStream fileLock = LockFiles();
        SyncState state = ReadState();
        state.Request++;
        WriteState(state);
        return state.Request;
    }

    public bool IsRunning()
    {
        try
        {
            using FileStream workerLock = Acquire(Path.Combine(_directory, "worker.lock"), 0);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public void StartBackground()
    {
        if (IsRunning()) return;
        try
        {
            string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate todo executable.");
            ProcessStartInfo start = new(executable)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = _repoPath,
            };
            if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            start.ArgumentList.Add("--sync-worker");
            start.ArgumentList.Add(_repoPath);
            using Process? process = Process.Start(start);
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or InvalidOperationException)
        {
            using FileStream fileLock = LockFiles();
            SyncState state = ReadState();
            state.LastError = $"Could not start background sync: {exception.Message}. Run todo sync to retry.";
            WriteState(state);
            Console.Error.WriteLine("Tasks saved locally; background sync could not start. Run todo status or todo sync.");
        }
    }

    public bool Synchronize(bool background, long? requested = null)
    {
        FileStream? workerLock;
        try
        {
            workerLock = Acquire(Path.Combine(_directory, "worker.lock"), background ? 0 : 120_000);
        }
        catch (IOException) when (background)
        {
            return true; // The current worker will see the durable request.
        }

        try
        {
            if (background) Thread.Sleep(500); // Coalesce bursts while reserving the single worker.
            while (true)
            {
                long request;
                long revision;
                string commit;
                GitSyncTarget? target;
                try
                {
                    using (FileStream fileLock = LockFiles())
                    {
                        SyncState state = ReadState();
                        request = state.Request;
                        if (requested is not null && state.CompletedRequest >= requested && state.LastError is null)
                        {
                            workerLock.Dispose();
                            workerLock = null;
                            return true;
                        }
                        target = _git.PrepareSync();
                        if (target is null)
                        {
                            state.CompletedRequest = request;
                            state.LastError = null;
                            state.Note = "Local only: no Git remote configured.";
                            WriteState(state);
                            workerLock.Dispose();
                            workerLock = null;
                            return true;
                        }
                        _git.CommitPending("Save pending todo changes");
                    }

                    // Fetch and push never hold the task-file lock.
                    string remoteCommit = _git.Fetch(target);
                    using (FileStream fileLock = LockFiles())
                    {
                        _git.EnsureNoConflicts();
                        _git.EnsureTargetUnchanged(target);
                        // Include edits made while fetching, before updating the working tree.
                        _git.CommitPending("Save pending todo changes");
                        _git.Integrate(remoteCommit);
                        commit = _git.Head();
                        SyncState state = ReadState();
                        revision = state.Revision;
                        request = state.Request;
                    }
                    _git.PushCommit(target, commit);

                    using (FileStream fileLock = LockFiles())
                    {
                        SyncState state = ReadState();
                        state.SyncedRevision = revision;
                        state.CompletedRequest = request;
                        state.LastSuccess = DateTimeOffset.Now;
                        state.LastError = null;
                        state.Note = "Synchronized.";
                        WriteState(state);
                        if (state.Request == request)
                        {
                            // Release the worker lock before writers can queue another request.
                            workerLock.Dispose();
                            workerLock = null;
                            return true;
                        }
                    }
                }
                catch (Exception exception) when (exception is GitException or IOException or UnauthorizedAccessException)
                {
                    using FileStream fileLock = LockFiles();
                    SyncState state = ReadState();
                    state.LastError = exception.Message;
                    state.Note = "Sync failed; local tasks and commits are retained. Fix the Git problem, then run todo sync. Do not repeat task commands.";
                    WriteState(state);
                    workerLock.Dispose();
                    workerLock = null;
                    if (!background) Console.Error.WriteLine($"Error: {state.Note}\n{state.LastError}");
                    return false;
                }
            }
        }
        finally
        {
            workerLock?.Dispose();
        }
    }

    public void PrintStatus()
    {
        using FileStream fileLock = LockFiles();
        SyncState state = ReadState();
        Console.WriteLine(IsRunning() ? "Sync: running" : "Sync: idle");
        bool pending = state.Revision > state.SyncedRevision || state.Request > state.CompletedRequest || _git.HasPendingChanges();
        Console.WriteLine(pending ? "Changes: pending synchronization" : "Changes: no pending local changes detected");
        Console.WriteLine(state.LastSuccess is null ? "Last successful sync: never" : $"Last successful sync: {state.LastSuccess:O}");
        if (state.Note is not null) Console.WriteLine(state.Note);
        if (state.LastError is not null) Console.WriteLine($"Last error: {state.LastError}");
    }

    public static int RunWorker(string repoPath)
    {
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
        if (!OperatingSystem.IsWindows()) _ = setsid();
        return new SyncCoordinator(repoPath).Synchronize(background: true) ? 0 : 1;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int setsid();
}
