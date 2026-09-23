namespace todo;

internal static class Help
{
    public static bool IsRequested(string[] args) =>
        args.Length == 1 && args[0] is "-h" or "-help" or "--help";

    public const string Text = """
        todo - Manage your tasks from the terminal

        Usage:
          todo                         List local tasks (same as todo list).
          todo add [-i] "task text"     Add one task; -i marks it important.
          todo list                    Show local task numbers and completion status.
          todo done <index>            Complete a task and move it to its dated archive.
          todo delete <index>          Delete one task.
          todo delete --all            Delete every task without confirmation.
          todo sync                    Wait for pending changes to sync and fetch remote tasks.
          todo status                  Show pending changes, worker state, and sync errors.
          todo -h | -help | --help     Show this help.

        Options:
          -i, -important, --important  Mark a new task important (add only).
          -a, -all, --all              Delete all tasks (delete only).
          --offline                   Save locally without requesting background sync.
          -info, --info               Show additional information.
          -v, -verbose, --verbose     Show diagnostic output.

        Examples:
          todo add "Buy groceries"
          todo add -i "Finish assignment"
          todo list --offline
          todo done 1

        Important tasks appear first. Check the current list before using an index.
        Quote task descriptions containing spaces. Add/delete accept one task at a time.
        Completed tasks go to YYYY/month/dd-MM (for example 2026/january/23-01).
        Completion removes the task from the active list; done does not reopen it.
        Task changes save locally and request background sync; list never syncs.
        Run todo sync before listing when you need the latest remote tasks.
        Help does not create configuration or access the task repository.
        """;
}
