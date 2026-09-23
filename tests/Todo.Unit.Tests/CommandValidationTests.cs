using todo;

namespace Todo.Unit.Tests;

public class CommandValidationTests
{
    public static IEnumerable<object[]> InvalidCommands()
    {
        yield return [new string[] { "add" }];
        yield return [new string[] { "add", "-i" }];
        yield return [new string[] { "add", "" }];
        yield return [new string[] { "add", "   " }];
        yield return [new string[] { "add", "first", "second" }];
        yield return [new string[] { "delete" }];
        yield return [new string[] { "delete", "1", "2" }];
        yield return [new string[] { "delete", "--all", "1" }];
        yield return [new string[] { "delete", "0" }];
        yield return [new string[] { "delete", "-1" }];
        yield return [new string[] { "delete", "not-a-number" }];
        yield return [new string[] { "done" }];
        yield return [new string[] { "done", "--all" }];
        yield return [new string[] { "done", "1", "2" }];
        yield return [new string[] { "done", "2147483648" }];
        yield return [new string[] { "list", "extra" }];
        yield return [new string[] { "list", "--unknown" }];
        yield return [new string[] { "unknown" }];
        yield return [new string[] { "2" }];
        yield return [new string[] { "99" }];
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void InvalidCommandsProduceActionableErrors(string[] args)
    {
        CommandLineException error = Assert.Throws<CommandLineException>(() => ArgumentParser.parseArgs(args));
        Assert.Contains("Use", error.Message);
    }

    [Theory]
    [InlineData(new string[] { }, Command.List)]
    [InlineData(new string[] { "list", "--verbose" }, Command.List)]
    [InlineData(new string[] { "list", "--info", "-v" }, Command.List)]
    [InlineData(new string[] { "ADD", "-i", "A task with spaces" }, Command.Add)]
    [InlineData(new string[] { "delete", "--all" }, Command.Delete)]
    [InlineData(new string[] { "delete", "1" }, Command.Delete)]
    [InlineData(new string[] { "done", "1", "--verbose" }, Command.Done)]
    public void ValidCommandsAreAccepted(string[] args, Command expected)
    {
        Assert.Equal(expected, ArgumentParser.parseArgs(args).Command);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, int.MinValue)]
    [InlineData(1, int.MaxValue)]
    public void InvalidIndicesLeaveTasksUnchanged(int count, int index)
    {
        string path = Path.GetTempFileName();
        try
        {
            MyFile file = new(path);
            for (int i = 0; i < count; i++)
            {
                file.Append(Classification.Regular, $"Task {i}");
            }
            file.Save();
            string before = File.ReadAllText(path);

            Assert.False(file.Delete(index));
            Assert.False(file.ToggleFinished(index));
            file.Save();

            Assert.Equal(before, File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LastValidIndexCanBeToggledAndDeleted()
    {
        string path = Path.GetTempFileName();
        try
        {
            MyFile file = new(path);
            file.Append(Classification.Regular, "First");
            file.Append(Classification.Regular, "Last");
            Assert.True(file.ToggleFinished(2));
            file.Save();
            Assert.Equal("Regular x: Last", File.ReadAllLines(path)[1]);
            Assert.True(file.Delete(2));
            file.Save();
            Assert.Equal("Regular _: First", File.ReadAllLines(path).Single());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
