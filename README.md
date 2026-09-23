# todo

A small command-line task manager. Add tasks, mark important ones, check them off, and optionally keep them synchronized through your own Git repository.

## Command-line help

Run `todo -h`, `todo -help`, or `todo --help` to see the available commands, options, and examples. Help works before first-run setup and does not read configuration, change tasks, or contact Git.

```sh
todo -h
```

## Getting started

Follow steps 1–3 to use the app locally. Continue with step 4 if you have a Git repository where you want to store your tasks.

### 1. Install the prerequisites

Install these two tools for your operating system:

- **[.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**: choose an SDK installer or the Linux package-manager instructions. The SDK includes what you need to build and run this app; installing only a runtime is not enough to build it.
- **[Git](https://git-scm.com/downloads/)**: follow the instructions for Windows, macOS, or Linux.

Open a new terminal after installation: **PowerShell** on Windows, or **Terminal** on macOS/Linux. Run each command separately:

```sh
dotnet --version
git --version
```

Both should print a version. The instructions below use the .NET 10 SDK. If either command is not found, finish its installation and reopen the terminal before continuing.

### 2. Download and install the app

Choose a folder for the application source, then run:

```sh
git clone https://github.com/Stefannaeve/todo.git todo-app
cd todo-app
dotnet pack src/todo/todo.csproj -c Release -o ./nupkg
dotnet tool install --global stefan.todo --source ./nupkg
```

These commands download the source, build an installable package, and install the `todo` command for your user account. If you already downloaded this repository, open a terminal in its root folder (the folder containing `todo.slnx`) and run just the last two commands. Internet access is needed to download the source and any build dependencies.

If installation says the tool is already installed, see **Updating or removing the app** below. If `todo` is not found after installation, see **Troubleshooting**.

### 3. Run it once

```sh
todo
```

An empty list is expected. This first run creates the configuration, task directory, and local Git repository automatically. The task file is created when you first add a task. You do not need a Git account, Git identity, or remote repository for local use.

You can now skip to **Your first tasks**, or connect your task repository below. Connect the repository before adding tasks if you want those tasks stored there; switching the configuration later does not move tasks from the default location.

### 4. Connect your task repository (optional)

You use two repositories for different purposes: `todo-app` contains this application's source code; your **task repository** contains your personal `todo.txt`. The steps below connect the second one.

**Clone your task repository.** Starting inside `todo-app`, move to its parent folder and clone the task repository alongside it. Replace the placeholder between the quotes with your repository's HTTPS or SSH clone URL:

```sh
cd ..
git clone "PASTE-YOUR-TASK-REPOSITORY-URL-HERE" my-tasks
cd my-tasks
```

You need permission to push to this repository. For a private repository, follow your Git hosting provider's sign-in instructions when Git asks. HTTPS usually uses a credential manager or access token; SSH requires an SSH key registered with your provider. The app itself does not prompt for credentials, so set up authentication with Git first.

**Set your commit identity.** Replace the example values with your name and email. These settings apply only to this task repository:

```sh
git config user.name "Your Name"
git config user.email "you@example.com"
```

**If the repository is empty**, create its first commit and set the remote tracking branch:

```sh
git commit --allow-empty -m "Initialize task repository"
git push -u origin HEAD
```

Skip those two commands if the repository already has commits. A normal clone of an existing branch already tracks its remote branch. You do not need to create `todo.txt` yourself; the app creates and tracks it when you add tasks.

Check that Git can authenticate for pushing:

```sh
git push --dry-run
```

This checks the push without uploading changes. Resolve any authentication or permission error before continuing. If Git says the branch has no upstream, `git push -u origin HEAD` publishes the current branch and establishes its tracking relationship.

**Find the full path of your task folder.** While still in `my-tasks`, run:

```sh
pwd
```

Copy the absolute path it displays. Next, open `config.json` in a plain-text editor such as Notepad or VS Code. Step 3 created it in this location:

| Operating system | Configuration file |
| --- | --- |
| Windows | `%APPDATA%\stefan.todo\config.json` — paste this path into File Explorer's address bar |
| macOS | `~/Library/Application Support/stefan.todo/config.json` — use Finder's **Go → Go to Folder** |
| Linux | `~/.config/stefan.todo/config.json` (or `$XDG_CONFIG_HOME/stefan.todo/config.json` if you configured that variable) |

The macOS location follows [.NET's application-data folder behavior](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/8.0/getfolderpath-unix).

Change `TodoPath` to the full path of your `my-tasks` folder. For example, on Linux:

```json
{
  "TodoPath": "/home/alex/my-tasks"
}
```

On macOS, a typical value is `/Users/alex/my-tasks`. On Windows, use forward slashes in the JSON to avoid escaping backslashes:

```json
{
  "TodoPath": "C:/Users/Alex/my-tasks"
}
```

Use your actual path, keep the quotes and braces, and save the file. Do not use `~` or environment variables inside the JSON; the app expects the full folder path. Point to the folder, not to `todo.txt` itself.

Finally, synchronize once and list your tasks:

```sh
todo sync
todo
```

The app now reads from your chosen local task repository. Task changes save immediately and request background synchronization. Listing tasks never contacts the remote; run `todo sync` first when you need the latest remote changes. Existing tasks must use this app's text format, for example `Regular _: Buy groceries` or `Important x: Finished task`; arbitrary text or Markdown task lists are not supported.

### 5. Your first tasks

Run these commands one at a time:

```sh
todo add "Buy groceries"
todo add -i "Finish assignment"
todo
```

The list shows important tasks first. Use the number from the current list to change a task:

```sh
todo done 1
todo
```

`done` completes the task, removes it from the active list, and appends it to the archive for today. Use `todo undo` to reverse the most recent completion or deletion. Numbers can change after adding, deleting, or completing a task, so check the list before using an index.

| What you want to do | Command |
| --- | --- |
| List local tasks | `todo` or `todo list` |
| Add a task | `todo add "Task description"` |
| Add an important task | `todo add -i "Task description"` |
| Change an active task's text | `todo edit 1 "New description"` |
| Complete and archive a task | `todo done 1` |
| Undo the most recent completion or deletion | `todo undo` |
| Delete one task | `todo delete 1` |
| Delete **all** tasks, without a confirmation prompt | `todo delete --all` |
| Synchronize now and wait for completion | `todo sync` |
| Check pending changes and synchronization errors | `todo status` |
| Read tasks without contacting the remote | `todo list` |
| Add a task without requesting background sync | `todo add --offline "Task description"` |
| Show diagnostic output | `todo list --verbose` |

Quotes are optional for ordinary task text. All remaining words form one task. Put options before the text, and before the index for `edit`. Deleting multiple indices in one command is not supported; `delete --all` clears the active list.

## Completed task archive

For example, finishing task 1 on January 23, 2026:

```sh
todo done 1
```

moves that task out of `todo.txt` and appends it to this file inside your configured task repository:

```text
my-tasks/
├── todo.txt
└── 2026/
    └── january/
        └── 23-01
```

The file name is `dd-MM`, with no extension. The year and date use your computer's local date at completion time; month folders always use lowercase English names. The folders and daily file are created automatically. Further tasks completed that day are appended to the same file, preserving existing entries. A different day, month, or year gets its own path.

Each archived line keeps the task's priority and text, with a completed marker:

```text
Important x: Finish assignment
Regular x: Buy groceries
```

Active tasks are still stored in `todo.txt`. `todo delete` only removes active tasks; it does not delete your completion history. Existing tasks marked `x` in `todo.txt` are not migrated automatically, because their original completion dates are unknown; using `done` on one archives it under today's date.

The background worker commits the active list and pending archive files together before synchronizing. `todo done 1 --offline` performs the same move without requesting a worker; a later `todo sync` or modifying command includes pending archives. Merely listing tasks does not request synchronization. Keep dated archives out of `.gitignore`, so Git can track your completion history. Files matching the `yyyy/month/dd-MM` archive layout are treated as task data; other repository files are excluded from task commits.

If the archive cannot be written, the task stays active. Completion also stops when `todo.txt` has malformed lines, so it cannot silently discard them. `done` is no longer a completion toggle: running it again on the same number may complete the next task now occupying that position.

## Troubleshooting

**`todo` is not found after installation.** Reopen your terminal first. If that does not help, add the .NET tool folder to the current terminal's `PATH`:

Linux/macOS (Bash or Zsh):

```sh
export PATH="$PATH:$HOME/.dotnet/tools"
```

Windows PowerShell:

```powershell
$env:Path += ";$env:USERPROFILE\.dotnet\tools"
```

For a permanent fix, add the same folder to your user PATH on Windows, or put the `export` line in `~/.bashrc` (Bash) or `~/.zshrc` (Zsh). See Microsoft's [global tool installation documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install).

**Git authentication or connection fails.** Task changes are still saved locally. Run `todo status` to read the last error, fix authentication or connectivity with Git, then run `todo sync`. Another modifying command also requests a new attempt. A failed worker exits; there is no periodic retry loop.

**The app reports conflicts or diverged history.** Local tasks and commits are retained. Reconcile the repository with Git, then run `todo sync`. Check `todo status` and wait for an active worker to finish before performing manual Git operations. Existing conflicts or unfinished Git operations block task modifications, including offline modifications, but you can still inspect the local list. The app never force-pushes, resets, or automatically resolves conflicts.

**A task command succeeded, but its changes are not on the remote.** Success means the local files were saved; background synchronization may still be running or may have failed. Use `todo status` for details or `todo sync` to wait for a result. Do not repeat the task operation just to retry synchronization: it could duplicate an addition or complete the next task at the same index.

## Updating or removing the app

To install a newer source version, open the `todo-app` folder and run:

```sh
git pull --ff-only
dotnet pack src/todo/todo.csproj -c Release -o ./nupkg
dotnet tool uninstall --global stefan.todo
dotnet tool install --global stefan.todo --source ./nupkg --no-cache
```

To remove only the installed command:

```sh
dotnet tool uninstall --global stefan.todo
```

Uninstalling the tool leaves your task repository and configuration in place.

## Development

From the application repository's root:

```sh
dotnet build -c Release
dotnet test
dotnet run --project src/todo -- list --offline
```

Running from source uses the same configuration as the installed tool unless you set `DOTNET_ENVIRONMENT=Development`.

## Command validation

`add` joins all words after leading options into one nonblank task description. `edit` joins everything after its index into the replacement text. `delete` takes one positive index or `--all`, and `done` takes one positive index (`done --all` is unsupported). `list` accepts `--info` and `--verbose`. Unsupported leading flags and missing required arguments produce an error on stderr and exit code 1 before setup or synchronization. An index outside the current list also exits with code 1 without saving task changes.

## Local commands and background synchronization

- `todo`, `todo list`, and `todo status` use local data and never fetch or push. On a fresh install, an empty local list is expected. Remote changes appear after a successful background sync or an explicit `todo sync`.
- `todo add`, `todo edit`, `todo done`, `todo undo`, and `todo delete` save locally before returning, then launch a separate background process. It keeps running after the command exits and is detached from the terminal. Closing the terminal does not intentionally cancel it; shutting down the machine stops it, but the saved files and pending request remain.
- The worker waits about half a second to group nearby changes. Only one worker synchronizes a given task repository at a time. Task-file operations use a separate lock, held only during local reads, writes, commits, and fast-forward updates. Fetch and push do not hold that lock, so network latency does not block local commands. A slow local Git hook can still delay local file access.
- `todo sync` waits for synchronization to finish and returns exit code 1 if it fails. If a running worker services the request successfully, the command reuses that result. Otherwise it performs synchronization itself. It commits pending task/archive changes, fetches the upstream, fast-forwards when possible, and pushes the captured commit. Edits made during network operations remain safe; newly requested changes trigger another pass.
- `todo status` reports whether a worker is running, whether app changes are pending, the last successful sync time, and the last error. Status uses local Git state; it cannot tell whether the remote has changed without synchronizing. Sync state and lock files live under `.git/todo-sync` (or the worktree's Git metadata directory), not in tracked task files.
- `--offline` on a modifying command saves locally without requesting a new background sync. It does not cancel a worker that is already running; that worker may include concurrent edits. A later modifying command or `todo sync` includes all pending task data. For list, `--offline` is accepted for compatibility but is unnecessary. `todo sync --offline` is rejected.
- Without a Git remote, tasks stay local and do not require a Git identity. With a remote, configure authentication, identity, and the branch upstream. Git credential prompts are disabled, and each Git subprocess has a 30-second timeout.
- Synchronization stages and commits only `todo.txt` and recognized dated archives, including previously untracked files and offline completions. Unrelated staged files are preserved. The fetch and push target the same upstream branch.
- Fetch or push failures are recorded for `todo status`; subsequent listings also show a short warning. Local task files and any local commits are retained. Fix the problem and use `todo sync`, or make another task change to request a retry. There is no persistent daemon or scheduled retry when no commands are being used.
- The existing fast-forward-only policy remains. If local and remote branches have both advanced, synchronization reports divergence and leaves reconciliation to you, even if a merge might be possible. There are no automatic merges, rebases, stashes, resets, or force pushes.
- Requests are persisted before changing task files, and OS-held locks are released if a worker exits or crashes. After restarting the computer or recovering from a worker failure, run `todo sync` to retry pending work. Synchronization failures never require repeating the original add/delete/done command.

## Undo the last completion or deletion

`done`, `delete`, and `delete --all` display an undo hint immediately:

```text
Deleted: Buy groceries
Undo: todo undo
```

Run `todo undo` to reverse the latest successful completion or deletion, even in a new terminal session. A deleted task is restored with its original priority and completion flag; `delete --all` can be undone as a batch. Tasks added or edited afterward are preserved. Undoing a completion removes its archive entry and restores it to the active list. `todo undo --offline` restores locally without requesting background synchronization.

There is only one shared undo slot. Each successful `done`, `delete`, or nonempty `delete --all` replaces it, and successful undo consumes it. Add, edit, list, sync, failed commands, and an empty `delete --all` leave it unchanged. There is no undo history or redo. The record stays in local Git metadata and is not synchronized between computers.

If a completed task's daily archive changed after completion, undo stops rather than overwriting those changes. Deletion undo does not touch archives. An empty daily file may remain after undoing its only completion. Retry synchronization with `todo sync`, not by repeating undo.

## Edit a task

Use the number from `todo list`, followed by the replacement text:

```sh
todo edit 1 Buy groceries and milk
```

This updates only the selected active task's text, preserving its priority, position, and completion flag. It saves locally immediately and requests background synchronization. Use `todo edit --offline 1 "New text"` to skip requesting a worker.

The replacement must be one nonblank line. A missing index, missing text, blank or multiline text, and invalid indices are rejected without changing the task file. Editing does not change archives or replace the undo slot; `todo undo` reverses the last completion or deletion, not the edit.

## Text without quotation marks

```sh
todo add Buy groceries and milk
todo add -i --offline Finish assignment
todo edit 1 Buy groceries tomorrow
todo edit --offline 1 Buy groceries tomorrow
todo add -- --offline is part of this task
```

For `add`, leading options are parsed until the first text word. For `edit`, options must come before the index; everything after the index is text. Once text starts, even `--offline` or `-i` is literal task text, not an option. Unquoted words are joined with single spaces. Quoted text still works and can preserve repeated spaces.

Your shell still interprets special characters such as `&`, `;`, `$`, and wildcards. Quote or escape those when you want them literally in the task. The app cannot change shell parsing.
