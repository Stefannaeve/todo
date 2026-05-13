namespace todo.Extensions;

internal static class ConditionExtensions {
    
    // string he "implicit converstion" te ReadOnlySpan<char> så vi treng ikkje den her. det e nok mæ bære den under
    extension(string s) {
        public Condition ToCondition() => s.AsSpan().ToCondition();
    }
    
    extension(ReadOnlySpan<char> span) {
        public Condition ToCondition() =>
            Enum.TryParse(span, ignoreCase: true, out Condition cond) ? cond : Condition.Unknown;
    }
}