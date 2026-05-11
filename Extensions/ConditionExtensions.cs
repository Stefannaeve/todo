namespace todo.Extensions;

public static class ConditionExtensions
{
    extension(string s)
    {
        public Condition ToCondition() => Enum.TryParse(s, out Condition cond) ? cond : Condition.Unknown;
    }
}
