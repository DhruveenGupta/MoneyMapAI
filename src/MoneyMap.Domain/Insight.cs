namespace MoneyMap.Domain;

public enum InsightSeverity
{
    Info,
    Positive,
    Warning,
    Critical,
}

/// <summary>
/// A single, reproducible observation derived purely from the user's own data. Insights never
/// shame the user or draw conclusions the numbers don't support — they state a fact, the
/// assumptions behind it, and (when relevant) what information is missing.
/// </summary>
public sealed record Insight
{
    public required string Key { get; init; }

    public required InsightSeverity Severity { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<string> Assumptions { get; init; } = Array.Empty<string>();

    public ExpenseCategory? RelatedCategory { get; init; }
}
