namespace MoneyMap.Domain;

public sealed record CategoryBreakdown
{
    public required ExpenseCategory Category { get; init; }

    public required Money PlannedTotal { get; init; }

    public Money? ActualTotal { get; init; }

    /// <summary>Share of total monthly net income this category's planned amount represents, or null if income is zero.</summary>
    public decimal? PercentOfIncome { get; init; }

    /// <summary>Share of total planned expenses this category represents, or null if there are no planned expenses.</summary>
    public decimal? PercentOfPlannedExpenses { get; init; }
}

/// <summary>
/// The full set of reconciled, derived figures for a budget at a point in time. Every number here
/// is computed directly from <see cref="Budget"/> with no external input, so a dashboard, a chart,
/// and an AI explanation that all read from the same <see cref="BudgetSummary"/> can never disagree.
/// </summary>
public sealed record BudgetSummary
{
    public required string Currency { get; init; }

    public required Money TotalMonthlyNetIncome { get; init; }

    public required Money TotalPlannedExpenses { get; init; }

    /// <summary>Sum of recorded actuals. Null (not zero) when no expense line has an actual recorded yet.</summary>
    public Money? TotalActualExpenses { get; init; }

    public required Money PlannedRemainingFunds { get; init; }

    /// <summary>Income minus (actual-if-recorded, else planned) for every line — the best current estimate of funds left.</summary>
    public required Money BlendedRemainingFunds { get; init; }

    /// <summary>Planned expenses as a percent of income, or null if income is zero.</summary>
    public decimal? BudgetUtilizationPercent { get; init; }

    public required IReadOnlyList<CategoryBreakdown> CategoryBreakdown { get; init; }

    public required IReadOnlyList<BudgetLine> OverBudgetLines { get; init; }

    public bool HasIncome => TotalMonthlyNetIncome.IsPositive;

    public bool HasAnyActuals => TotalActualExpenses.HasValue;

    public bool PlannedExceedsIncome => PlannedRemainingFunds.IsNegative;
}
