using MoneyMap.Domain;

namespace MoneyMap.Application.Ai;

public sealed record AiCategorySnapshot
{
    public required ExpenseCategory Category { get; init; }

    public required decimal PlannedAmount { get; init; }

    public decimal? ActualAmount { get; init; }

    public decimal? PercentOfIncome { get; init; }
}

public sealed record AiGoalSnapshot
{
    public required string Name { get; init; }

    public required decimal TargetAmount { get; init; }

    public required decimal CurrentAmount { get; init; }

    public DateOnly? Deadline { get; init; }

    public decimal? RequiredMonthlyContribution { get; init; }

    public decimal? PlannedMonthlyContribution { get; init; }
}

/// <summary>
/// The minimum financial context required to answer a question, assembled from
/// <see cref="BudgetSummary"/> and goal projections. Deliberately excludes free-text notes and any
/// data not needed to explain the budget, in line with "send only the minimum necessary context."
/// </summary>
public sealed record AiFinancialContext
{
    public required string Currency { get; init; }

    public required decimal MonthlyNetIncome { get; init; }

    public required decimal PlannedExpenses { get; init; }

    public decimal? ActualExpenses { get; init; }

    public required decimal PlannedRemainingFunds { get; init; }

    public decimal? BudgetUtilizationPercent { get; init; }

    public IReadOnlyList<AiCategorySnapshot> Categories { get; init; } = Array.Empty<AiCategorySnapshot>();

    public IReadOnlyList<string> OverBudgetCategoryNames { get; init; } = Array.Empty<string>();

    public IReadOnlyList<AiGoalSnapshot> SavingsGoals { get; init; } = Array.Empty<AiGoalSnapshot>();

    public static AiFinancialContext Build(BudgetSummary summary, IReadOnlyList<SavingsGoal> goals, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(goals);

        var categories = summary.CategoryBreakdown.Select(c => new AiCategorySnapshot
        {
            Category = c.Category,
            PlannedAmount = c.PlannedTotal.Amount,
            ActualAmount = c.ActualTotal?.Amount,
            PercentOfIncome = c.PercentOfIncome,
        }).ToList();

        var overBudgetNames = summary.OverBudgetLines.Select(l => l.Name).Distinct().ToList();

        var goalSnapshots = goals.Select(g =>
        {
            var required = SavingsGoalCalculator.CalculateRequiredMonthlyContribution(g, asOf);
            return new AiGoalSnapshot
            {
                Name = g.Name,
                TargetAmount = g.TargetAmount.Amount,
                CurrentAmount = g.CurrentAmount.Amount,
                Deadline = g.Deadline,
                RequiredMonthlyContribution = required.RequiredMonthlyContribution?.Amount,
                PlannedMonthlyContribution = g.PlannedMonthlyContribution?.Amount,
            };
        }).ToList();

        return new AiFinancialContext
        {
            Currency = summary.Currency,
            MonthlyNetIncome = summary.TotalMonthlyNetIncome.Amount,
            PlannedExpenses = summary.TotalPlannedExpenses.Amount,
            ActualExpenses = summary.TotalActualExpenses?.Amount,
            PlannedRemainingFunds = summary.PlannedRemainingFunds.Amount,
            BudgetUtilizationPercent = summary.BudgetUtilizationPercent,
            Categories = categories,
            OverBudgetCategoryNames = overBudgetNames,
            SavingsGoals = goalSnapshots,
        };
    }
}
