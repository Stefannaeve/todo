using todo;

namespace Todo.Unit.Tests;

public class FreeTextTests
{
    [Fact]
    public void AddJoinsRemainingWordsAndTreatsTrailingFlagsAsText()
    {
        CommandArgument parsed = ArgumentParser.parseArgs(["add", "-i", "--offline", "Buy", "groceries", "--verbose"]);
        Assert.Contains(parsed.Arguments, argument => argument.ArgumentType == ArgumentType.Important);
        Assert.Contains(parsed.Arguments, argument => argument.ArgumentType == ArgumentType.Offline);
        Assert.DoesNotContain(parsed.Arguments, argument => argument.ArgumentType == ArgumentType.Verbose);
        Assert.Equal("Buy groceries --verbose", parsed.Arguments.Single(argument => argument.ArgumentType == ArgumentType.Value).Value);
    }

    [Fact]
    public void EditUsesEverythingAfterIndexAsReplacement()
    {
        CommandArgument parsed = ArgumentParser.parseArgs(["edit", "--offline", "2", "Buy", "milk", "-i"]);
        Assert.Equal(["2", "Buy milk -i"], parsed.Arguments.Where(argument => argument.ArgumentType == ArgumentType.Value).Select(argument => argument.Value).ToArray());
        Assert.DoesNotContain(parsed.Arguments, argument => argument.ArgumentType == ArgumentType.Important);
    }

    [Fact]
    public void QuotedTextStillPreservesWhitespace()
    {
        CommandArgument parsed = ArgumentParser.parseArgs(["add", "Buy  milk"]);
        Assert.Equal("Buy  milk", parsed.Arguments.Single().Value);
    }

    [Fact]
    public void SeparatorAllowsTextStartingWithDash()
    {
        CommandArgument parsed = ArgumentParser.parseArgs(["add", "--", "--offline", "is", "text"]);
        Assert.Equal("--offline is text", parsed.Arguments.Single().Value);
    }

    [Fact]
    public void AddRejectsMultilineText()
    {
        Assert.Throws<CommandLineException>(() => ArgumentParser.parseArgs(["add", "First\nSecond"]));
    }
}
