using todo;

namespace Todo.Unit.Tests;

public class ParserTests {

    [Fact]
    public void BasicParserTest() {

        List<string> strings = [
            "Important _: Seems to be working",
            "Regular x: Seems to be working"
        ];

        Parser parser = new();
        List<TodoItem> output = parser.ParseLines(strings).ToList();

        Assert.Equal(2, output.Count);
        Assert.Equal(1, output.Count(todoItem => !todoItem.Finished));
        Assert.Equal(1, output.Count(todoItem => todoItem.Finished));
    }

    [Fact]
    public void EmptyParserTest() {
        Parser parser = new();
        List<TodoItem> output = parser.ParseLines([]).ToList();
        Assert.Empty(output);
    }

    [Fact]
    public void IgnoreBlankLinesParserTest() {
        List<string> strings = [
            "",
            "Important _: Seems to be working",
            "",
            "Regular x: Seems to be working",
            ""
        ];

        Parser parser = new();
        List<TodoItem> output = parser.ParseLines(strings).ToList();

        Assert.Equal(2, output.Count);
        Assert.Equal(1, output.Count(todoItem => !todoItem.Finished));
        Assert.Equal(1, output.Count(todoItem => todoItem.Finished));

    }
    [Fact]
    public void ParserTest() {
        Parser parser = new();
        TodoItem? todoItem = parser.ParseLine("Important _: Seems to be working: or is it!?");
        Assert.NotNull(todoItem);
        Assert.Equal(Classification.Important, todoItem.Classification);
        Assert.False(todoItem.Finished);
        Assert.Equal("Seems to be working: or is it!?", todoItem.Body);
    }

    [Theory]
    [InlineData("Inportnt x: this is a body", "Unable to parse classification")]
    [InlineData("Important s: this is a body", "Unable to parse status")]
    [InlineData("Important x this is a body", "Metadata parts too long")]
    [InlineData("Important x awd :this is a body", "Metadata parts too long")]
    [InlineData("Important x", "Missing body")]
    public void InvalidParserTest(string input, string expectedMessage) {
        Parser parser = new();
        parser.ParseLine(input);
        IReadOnlyCollection<ParseError> errors = parser.ParseErrors;
        Assert.Single(errors);
        Assert.Contains(expectedMessage, errors.First().Message);
    }
}
