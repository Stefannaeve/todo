# todo

A small command-line task manager. Add tasks, mark important ones, check them off, and optionally keep them synchronized through your own Git repository.

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

An empty list is expected. This first run creates the configuration, task directory, local Git repository, and `todo.txt` file automatically. You do not need a Git account, Git identity, or remote repository for local use.

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

Finally, run:

```sh
todo
```

The app now reads from your chosen task repository. It pulls before reading tasks and commits/pushes task changes automatically. Existing tasks must use this app's text format, for example `Regular _: Buy groceries` or `Important x: Finished task`; arbitrary text or Markdown task lists are not supported.

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

`done` toggles completion: running it again on that task reopens it. Numbers can change when tasks are added or deleted, so check the list before using an index.

| What you want to do | Command |
| --- | --- |
| List tasks | `todo` or `todo list` |
| Add a task | `todo add "Task description"` |
| Add an important task | `todo add -i "Task description"` |
| Complete or reopen a task | `todo done 1` |
| Delete one task | `todo delete 1` |
| Delete **all** tasks, without a confirmation prompt | `todo delete --all` |
| Read tasks without contacting the remote | `todo list --offline` |
| Add a task locally without synchronizing | `todo add "Task description" --offline` |
| Show diagnostic output | `todo list --verbose` |

Quote task descriptions containing spaces. Adding or deleting multiple tasks in one command is not supported.

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

**Git authentication or connection fails.** From your `my-tasks` folder, check `git fetch` and `git push --dry-run`. Fix the credentials or connection there, or explicitly use `--offline` to work with the local list. Offline changes are saved but not committed or pushed.

**The app reports conflicts or diverged history.** Resolve the repository with Git before resuming synchronization. Existing conflicts or unfinished Git operations block task commands even offline. The app will not merge or reset your data automatically.

**The app says tasks were saved locally but synchronization failed.** Do not repeat the task command: an addition could be duplicated, or completion toggled again. Fix the reported Git problem and synchronize the saved changes manually. See the failure policy below.

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

`add` takes exactly one nonblank task description; quote descriptions containing spaces. `delete` takes one positive index or `--all`, and `done` takes one positive index (`done --all` is unsupported). `list` accepts `--info` and `--verbose`. Extra values, unsupported flags, and missing arguments produce an error on stderr and exit code 1 before setup or synchronization. An index outside the current list also exits with code 1 without saving task changes.

## Offline use and synchronization failures

- Without a remote, tasks remain local; no identity, commits, pulls, or pushes are required.
- With a remote, normal commands pull from the current branch's upstream before reading tasks. Pulls are fast-forward only, even when your Git configuration requests a rebase. A failed pull stops the command before applying task changes; the tool never automatically falls back to a stale local list.
- Use `todo list --offline` or `todo add "A local task" --offline` to explicitly skip pull, commit, and push. Offline writes are saved in `todo.txt` and remain uncommitted. No remote or upstream is required for this mode. Git must still be installed.
- A diverged branch is left for you to reconcile with Git. An existing conflict or in-progress merge/rebase/cherry-pick/revert blocks all task commands, even offline, so the tool cannot rewrite conflict markers or pending resolutions. There are no automatic resets, stashes, conflict resolutions, or force pushes.
- Changes stage and commit only `todo.txt`, including a previously untracked file. Unrelated staged files are left out of the commit. Push targets the same upstream branch used by pull.
- If staging, committing, or pushing fails after a save, the task file is retained and the error explicitly says it was saved locally. Do not repeat the task command, as that could duplicate an addition or toggle completion again. Correct the reported problem and synchronize manually. After a failed push, the local commit remains; retry pushing to the configured upstream after resolving the failure. After a failed commit or offline edits, commit the task file first and reconcile any remote changes before pushing.
- A remote without a configured upstream requires Git setup before normal commands can run; `--offline` remains available. Git credential prompts are disabled, and each Git subprocess has a 30-second timeout so synchronization cannot hang indefinitely.
