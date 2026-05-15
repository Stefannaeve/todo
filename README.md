dotnet tool install -g stefan.todo --source .\nupkg\

dotnet tool uninstall stefan.todo -g

dotnet pack

dotnet build -c Release

dotnet tool list -g

dotnet run --

dnx stefan.todo --source ./nupkg -- add

dnx = dotnet tool exec