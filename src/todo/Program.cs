using System.Text.Json;
using todo.Extensions;
using todo.HelperClasses;

namespace todo;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 2 && args[0] == "--sync-worker")
                return SyncCoordinator.RunWorker(args[1]);
            return Run(args);
        }
        catch (Exception exception) when (exception is GitException or InvalidDataException or IOException or UnauthorizedAccessException or JsonException)
        {
            return Error(exception.Message);
        }
    }

    private static int Run(string[] args)
    {
        if (Help.IsRequested(args))
        {
            Console.WriteLine(Help.Text);
            return 0;
        }

        CommandArgument commandArgument;
        try
        {
            commandArgument = ArgumentParser.parseArgs(args);
        }
        catch (CommandLineException exception)
        {
            return Error(exception.Message);
        }

        if (!ConfigHandler.ConfigExists()) ConfigHandler.CreateConfig();
        Config config = IsDevelopment() ? new Config("todo.txt") : ConfigHandler.LoadConfig()
            ?? throw new InvalidOperationException("Config not found");
        Message.InfoEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Info);
        Message.VerboseEnabled = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Verbose);

        Git git = new(config.TodoPath);
        git.EnsureInitialized();
        SyncCoordinator sync = new(config.TodoPath);
        if (commandArgument.Command == Command.Sync)
        {
            long request = sync.RequestSync();
            if (!sync.Synchronize(background: false, requested: request)) return 1;
            using FileStream statusLock = sync.LockFiles();
            Console.WriteLine(sync.ReadState().Note);
            return 0;
        }
        if (commandArgument.Command == Command.Status)
        {
            sync.PrintStatus();
            return 0;
        }

        bool offline = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Offline);
        using (FileStream fileLock = sync.LockFiles())
        {
            MyFile file = new(Path.Combine(config.TodoPath, "todo.txt"));
            if (File.Exists(Path.Combine(config.TodoPath, "todo.txt"))) file.ParseFile();
            if (commandArgument.Command == Command.List)
            {
                file.WriteTodos();
                if (file.ParseErrors.Count > 0)
                    Console.Error.WriteLine("Some task lines could not be read. Fix todo.txt before changing tasks.");
                if (sync.ReadState().LastError is not null)
                    Console.Error.WriteLine("The last sync failed. Showing local tasks; run todo status for details.");
                return 0;
            }

            git.EnsureNoConflicts();
            if (file.ParseErrors.Count > 0)
                return Error("Cannot change tasks while todo.txt contains invalid lines. Fix those lines first; no tasks were changed.");

            string? value = commandArgument.Arguments.FirstOrDefault(argument => argument.ArgumentType == ArgumentType.Value)?.Value;
            bool deleteAll = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.All);
            int index = 0;
            if (commandArgument.Command is Command.Done or Command.Edit || (commandArgument.Command == Command.Delete && !deleteAll))
            {
                if (!int.TryParse(value, out index) || index < 1 || index > file.Count)
                    return Error($"Task {value} does not exist. Use todo list to see available indices.");
            }

            CompletionUndo undo = new(config.TodoPath, git.MetadataPath);
            if (commandArgument.Command == Command.Undo) undo.Validate();

            // Persist the request before saving, so termination cannot lose the sync request.
            sync.MarkPending(requestSync: !offline);
            switch (commandArgument.Command)
            {
                case Command.Add:
                    Classification classification = commandArgument.Arguments.Any(argument => argument.ArgumentType == ArgumentType.Important)
                        ? Classification.Important : Classification.Regular;
                    file.Append(classification, value!);
                    file.Save();
                    break;
                case Command.Edit:
                    string replacement = commandArgument.Arguments
                        .Where(argument => argument.ArgumentType == ArgumentType.Value).ElementAt(1).Value!;
                    file.Edit(index, replacement);
                    file.Save();
                    Console.WriteLine($"Updated: {replacement}");
                    break;
                case Command.Delete:
                    if (deleteAll) file.DeleteAll();
                    else file.Delete(index);
                    file.Save();
                    break;
                case Command.Done:
                    UndoEntry completed = undo.Complete(file, index, DateOnly.FromDateTime(DateTime.Now));
                    Console.WriteLine($"Completed: {completed.Item.Body}");
                    Console.WriteLine($"Archived in {completed.ArchivePath}. Undo: todo undo");
                    break;
                case Command.Undo:
                    UndoEntry restored = undo.Undo(file);
                    Console.WriteLine($"Restored: {restored.Item.Body}");
                    break;
                default:
                    throw new InvalidOperationException("Unsupported task command.");
            }
            if (sync.ReadState().LastError is not null)
                Console.Error.WriteLine("Saved locally. The last sync failed; run todo status for details.");
        }

        if (!offline) sync.StartBackground();
        return 0;
    }

    private static int Error(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
        return 1;
    }

    private static bool IsDevelopment() =>
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
}
