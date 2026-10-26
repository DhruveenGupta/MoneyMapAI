namespace MoneyMap.Domain;

public enum InsightSeverity
{
    Info,
    Positive,
    Warning,
    Critical,
}

// One observation derived from the user's own numbers - states a fact and its assumptions,
// doesn't moralize about spending habits.
public sealed record Insight
{
    public required string Key { get; init; }

    public required InsightSeverity Severity { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<string> Assumptions { get; init; } = Array.Empty<string>();

    public ExpenseCategory? RelatedCategory { get; init; }
}
