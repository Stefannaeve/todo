namespace todo;

internal static class Help
{
    public static bool IsRequested(string[] args) =>
        args.Length == 1 && args[0] is "-h" or "-help" or "--help";

    public const string Text = """
        todo - Manage your tasks from the terminal

        Usage:
          todo                         List tasks (same as todo list).
          todo add [-i] "task text"     Add one task; -i marks it important.
          todo list                    Show task numbers and completion status.
          todo done <index>            Complete a task and move it to its dated archive.
          todo delete <index>          Delete one task.
          todo delete --all            Delete every task without confirmation.
          todo -h | -help | --help     Show this help.

        Options:
          -i, -important, --important  Mark a new task important (add only).
          -a, -all, --all              Delete all tasks (delete only).
          --offline                   Use local tasks; skip pull, commit, and push.
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
        With a remote configured, normal commands synchronize through Git.
        Help does not create configuration or access the task repository.
        """;
}
