# todo

A command-line task list packaged as the `stefan.todo` .NET tool. Requires .NET 10 and Git on PATH.

## First run

Run `todo` to create the configuration, task directory, local Git repository, and empty `todo.txt` automatically. No Git identity or remote is needed for local use.

```sh
todo add "My first task"
todo add -i "An important task"
todo
todo done 1
```

Configuration is stored in `stefan.todo/config.json` beneath the operating system's application-data directory (normally `~/.config` on Linux). Its `TodoPath` defaults to the `todo` subdirectory beside that configuration file.

Existing task files and repositories are preserved. Without a remote, tasks are saved locally; commits, pulls, and pushes are skipped.

## Optional synchronization

To use an existing remote task repository, clone it with Git and set `TodoPath` in `config.json` to that clone's directory. Configure your Git identity, authentication, and branch upstream as usual. Once a remote exists, the tool pulls before reading tasks and commits/pushes changes. Git failures are printed on stderr with exit code 1, without a stack trace.

To publish a new local task list, configure a remote and make the initial commit and upstream push with Git before using synchronization.

## Development commands

dotnet tool install -g stefan.todo --source .\nupkg\

dotnet tool uninstall stefan.todo -g

dotnet pack

dotnet build -c Release

dotnet tool list -g

dotnet run --

dnx stefan.todo --source ./nupkg -- add

dnx = dotnet tool exec
## Command validation

`add` takes exactly one nonblank task description; quote descriptions containing spaces. `delete` takes one positive index or `--all`, and `done` takes one positive index (`done --all` is unsupported). `list` accepts `--info` and `--verbose`. Extra values, unsupported flags, and missing arguments produce an error on stderr and exit code 1 before setup or synchronization. An index outside the current list also exits with code 1 without saving task changes.

## Offline use and synchronization failures

- Without a remote, tasks remain local; no identity, commits, pulls, or pushes are required.
- With a remote, normal commands pull from the current branch's upstream before reading tasks. Pulls are fast-forward only, even when your Git configuration requests a rebase. A failed pull stops the command before applying task changes; the tool never automatically falls back to a stale local list.
- Use `todo list --offline` or `todo add "A local task" --offline` to explicitly skip pull, commit, and push. Offline writes are saved in `todo.txt` and remain uncommitted. No remote or upstream is required for this mode. Git must still be installed.
- A diverged branch is left for you to reconcile with Git. An existing conflict or in-progress merge/rebase/cherry-pick/revert blocks all task commands, even offline, so the tool cannot rewrite conflict markers or pending resolutions. There are no automatic resets, stashes, conflict resolutions, or force pushes.
- Changes stage and commit only `todo.txt`, including a previously untracked file. Unrelated staged files are left out of the commit. Push targets the same upstream branch used by pull.
- If staging, committing, or pushing fails after a save, the task file is retained and the error explicitly says it was saved locally. Do not repeat the task command, as that could duplicate an addition or toggle completion again. Correct the reported problem and synchronize manually. After a failed push, the local commit remains; retry pushing to the configured upstream after resolving the failure. After a failed commit or offline edits, commit the task file first and reconcile any remote changes before pushing.
- A remote without a configured upstream requires Git setup before normal commands can run; `--offline` remains available. Git credential prompts are disabled, and each Git subprocess has a 30-second timeout so synchronization cannot hang indefinitely.
