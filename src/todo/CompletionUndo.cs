using System.Security.Cryptography;
using System.Text.Json;

namespace todo;

public sealed record UndoEntry(TodoItem Item, int Index, string ArchivePath, byte[] PreviousArchive, string ArchiveHash);

public sealed class CompletionUndo(string repoPath, string metadataPath)
{
    private string RecordPath => Path.Combine(metadataPath, "todo-sync", "last-done.json");
    private string TaskPath => Path.Combine(repoPath, "todo.txt");

    // The caller holds SyncCoordinator.LockFiles for the entire operation.
    public UndoEntry Complete(MyFile file, int index, DateOnly date)
    {
        TodoItem item = file.GetItem(index);
        string relative = CompletionArchive.RelativePath(date);
        string archive = Path.Combine(repoPath, relative);
        bool existed = File.Exists(archive);
        byte[] beforeArchive = existed ? File.ReadAllBytes(archive) : [];
        byte[] beforeTasks = File.ReadAllBytes(TaskPath);
        file.Complete(index, date);
        try
        {
            UndoEntry entry = new(item, index, relative, beforeArchive, Hash(File.ReadAllBytes(archive)));
            WriteAtomic(RecordPath, JsonSerializer.SerializeToUtf8Bytes(entry));
            return entry;
        }
        catch
        {
            WriteAtomic(TaskPath, beforeTasks);
            if (existed) WriteAtomic(archive, beforeArchive);
            else File.Delete(archive);
            throw;
        }
    }

    public UndoEntry Validate()
    {
        if (!File.Exists(RecordPath)) throw new InvalidDataException("No completed task to undo.");
        UndoEntry? entry = JsonSerializer.Deserialize<UndoEntry>(File.ReadAllBytes(RecordPath));
        if (entry is null || entry.Item is null || entry.Item.Body is null || entry.PreviousArchive is null ||
            entry.Index < 1 || entry.ArchivePath is null || !CompletionArchive.IsArchivePath(entry.ArchivePath) ||
            entry.Item.Classification is not (Classification.Regular or Classification.Important))
            throw new InvalidDataException("The last completion record is invalid; no tasks were changed.");
        string archive = Path.Combine(repoPath, entry.ArchivePath);
        if (!File.Exists(archive) || Hash(File.ReadAllBytes(archive)) != entry.ArchiveHash)
            throw new InvalidDataException("The archive changed since the last completion. Undo was stopped to preserve its contents; restore the task manually.");
        return entry;
    }

    public UndoEntry Undo(MyFile file)
    {
        UndoEntry entry = Validate();
        string archive = Path.Combine(repoPath, entry.ArchivePath);
        byte[] beforeArchive = File.ReadAllBytes(archive);
        byte[] beforeTasks = File.ReadAllBytes(TaskPath);
        try
        {
            file.Restore(entry.Item, entry.Index);
            file.Save();
            // Keep an empty daily file if this was its only entry, so Git stages it normally.
            WriteAtomic(archive, entry.PreviousArchive);
            File.Delete(RecordPath);
            return entry;
        }
        catch
        {
            WriteAtomic(TaskPath, beforeTasks);
            WriteAtomic(archive, beforeArchive);
            throw;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    private static void WriteAtomic(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
