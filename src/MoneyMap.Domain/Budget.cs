namespace MoneyMap.Domain;

// Income, expenses, and savings for one user, all in one currency.
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
