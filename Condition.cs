namespace todo;

public enum Condition
{
    add,
    delete,
    deleteAll
}

public static class ConditionExtensions
{
    extension(Condition c)
    {
        public string ToText() => c.ToString();
    }

    extension(string s)
    {
        public Condition? ToCondition()
        {
            // Om du har en Condtion. Unknown elns så kan du unngå null. men skulle ikke skrive om for mye, bare vise fancy C# måte å gjøre ting på.
            _ = Enum.TryParse(s, out Condition cond);
            return cond;
        }
    }
}
