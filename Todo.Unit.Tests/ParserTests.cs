using todo;

namespace Todo.Unit.Tests;

public class ParserTests
{
    [Fact]
    public void BasicParserTest()
    {
        var myFile = new MyFile("test.txt");

        List<string> strings =
        [
            "Important _: Seems to be working",
            "Regular x: Seems to be working"
        ];


        var output = myFile.ParseTodoItemFromLine(strings);

        Assert.Equal(2, output.Count);
        Assert.Equal(1, output.Count(todoItem => !todoItem.Finished));
        Assert.Equal(1, output.Count(todoItem => todoItem.Finished));

    }
}
