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

To use an existing remote task repository, clone it with Git and set `TodoPath` in `config.json` to that clone's directory. Configure your Git identity, authentication, and branch upstream as usual. Once a remote exists, the tool pulls before reading tasks and commits/pushes changes. Git failures are reported rather than silently ignored.

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
