namespace MoneyMap.Domain;

/// <summary>
/// The user's budget: income sources, planned/actual expenses, and current savings, all in a
/// single currency. This is the single source of truth that every chart, summary figure, and
/// AI answer must be derived from.
/// </summary>
public sealed record Budget
{
    public required string Currency { get; init; }

    public IReadOnlyList<IncomeSource> IncomeSources { get; init; } = Array.Empty<IncomeSource>();

    public IReadOnlyList<BudgetLine> Expenses { get; init; } = Array.Empty<BudgetLine>();

    public Money CurrentSavings { get; init; }

    public Money? EmergencyFundTarget { get; init; }

    public static Budget Empty(string currency) => new()
    {
        Currency = currency,
        CurrentSavings = Money.Zero(currency),
    };
}
