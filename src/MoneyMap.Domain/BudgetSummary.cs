namespace MoneyMap.Domain;

public sealed record CategoryBreakdown
{
    public required ExpenseCategory Category { get; init; }

    public required Money PlannedTotal { get; init; }

    public Money? ActualTotal { get; init; }

    // Share of income this category's planned amount is, null if income is zero.
    public decimal? PercentOfIncome { get; init; }

    // Share of total planned expenses, null if there are no planned expenses.
    public decimal? PercentOfPlannedExpenses { get; init; }
}

// All the derived numbers for a budget at a point in time, computed straight from Budget.
public sealed record BudgetSummary
{
    public required string Currency { get; init; }

    public required Money TotalMonthlyNetIncome { get; init; }

    public required Money TotalPlannedExpenses { get; init; }

    // Sum of recorded actuals. Null, not zero, when nothing's been recorded yet.
    public Money? TotalActualExpenses { get; init; }

    public required Money PlannedRemainingFunds { get; init; }

    // Income minus actual-if-recorded-else-planned for every line.
    public required Money BlendedRemainingFunds { get; init; }

    // Planned expenses as a percent of income, null if income is zero.
    public decimal? BudgetUtilizationPercent { get; init; }

    public required IReadOnlyList<CategoryBreakdown> CategoryBreakdown { get; init; }

    public required IReadOnlyList<BudgetLine> OverBudgetLines { get; init; }

    public bool HasIncome => TotalMonthlyNetIncome.IsPositive;

    public bool HasAnyActuals => TotalActualExpenses.HasValue;

    public bool PlannedExceedsIncome => PlannedRemainingFunds.IsNegative;
}
