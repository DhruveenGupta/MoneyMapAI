namespace MoneyMap.Domain;

/// <summary>
/// Pure, deterministic budget math. No randomness, no I/O, no LLM involvement — every figure the
/// dashboard shows traces back to a method here.
/// </summary>
public static class BudgetCalculator
{
    public static BudgetSummary Summarize(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        var currency = budget.Currency;
        var zero = Money.Zero(currency);

        var totalIncome = budget.IncomeSources
            .Where(i => i.IsActive)
            .Aggregate(zero, (sum, income) => sum + income.MonthlyNet);

        var totalPlanned = budget.Expenses.Aggregate(zero, (sum, line) => sum + line.PlannedAmount);

        var actualLines = budget.Expenses.Where(l => l.HasActual).ToList();
        Money? totalActual = actualLines.Count > 0
            ? actualLines.Aggregate(zero, (sum, line) => sum + line.ActualAmount!.Value)
            : null;

        var blendedExpenses = budget.Expenses.Aggregate(zero, (sum, line) => sum + (line.ActualAmount ?? line.PlannedAmount));

        var plannedRemaining = totalIncome - totalPlanned;
        var blendedRemaining = totalIncome - blendedExpenses;

        decimal? utilization = totalIncome.IsPositive
            ? Math.Round(totalPlanned.Amount / totalIncome.Amount * 100m, 1, MidpointRounding.AwayFromZero)
            : null;

        var breakdown = budget.Expenses
            .GroupBy(l => l.Category)
            .Select(group =>
            {
                var plannedTotal = group.Aggregate(zero, (sum, line) => sum + line.PlannedAmount);
                var groupActuals = group.Where(l => l.HasActual).ToList();
                Money? actualTotal = groupActuals.Count > 0
                    ? groupActuals.Aggregate(zero, (sum, line) => sum + line.ActualAmount!.Value)
                    : null;

                return new CategoryBreakdown
                {
                    Category = group.Key,
                    PlannedTotal = plannedTotal,
                    ActualTotal = actualTotal,
                    PercentOfIncome = plannedTotal.PercentOf(totalIncome),
                    PercentOfPlannedExpenses = totalPlanned.IsPositive ? plannedTotal.PercentOf(totalPlanned) : null,
                };
            })
            .OrderByDescending(c => c.PlannedTotal.Amount)
            .ToList();

        var overBudget = budget.Expenses.Where(l => l.IsOverPlanned).ToList();

        return new BudgetSummary
        {
            Currency = currency,
            TotalMonthlyNetIncome = totalIncome,
            TotalPlannedExpenses = totalPlanned,
            TotalActualExpenses = totalActual,
            PlannedRemainingFunds = plannedRemaining,
            BlendedRemainingFunds = blendedRemaining,
            BudgetUtilizationPercent = utilization,
            CategoryBreakdown = breakdown,
            OverBudgetLines = overBudget,
        };
    }

    /// <summary>
    /// Replays the budget as if <paramref name="categoryDeltas"/> were applied to each category's
    /// planned total (positive = increase spending, negative = decrease), without mutating the
    /// original budget. Powers the "what if I spend $50 less on restaurants" budget-builder scenario.
    /// </summary>
    public static BudgetSummary SimulateCategoryChange(Budget budget, IReadOnlyDictionary<ExpenseCategory, decimal> categoryDeltas)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(categoryDeltas);

        return Summarize(ApplyCategoryDeltas(budget, categoryDeltas));
    }

    /// <summary>
    /// Returns a new <see cref="Budget"/> with each category's planned total shifted by its delta
    /// (positive = increase spending, negative = decrease), split evenly across that category's
    /// existing lines and never reduced below zero. Used both for what-if previews and, once a
    /// user explicitly confirms an AI-suggested change, for actually applying it.
    /// </summary>
    public static Budget ApplyCategoryDeltas(Budget budget, IReadOnlyDictionary<ExpenseCategory, decimal> categoryDeltas)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(categoryDeltas);

        if (categoryDeltas.Count == 0)
        {
            return budget;
        }

        var adjustedLines = new List<BudgetLine>(budget.Expenses.Count);

        foreach (var category in categoryDeltas.Keys)
        {
            var linesInCategory = budget.Expenses.Where(l => l.Category == category).ToList();
            if (linesInCategory.Count == 0)
            {
                continue;
            }

            var delta = categoryDeltas[category];
            var perLineShare = delta / linesInCategory.Count;
            foreach (var line in linesInCategory)
            {
                var newPlanned = Math.Max(0m, line.PlannedAmount.Amount + perLineShare);
                adjustedLines.Add(line.WithPlannedAmount(newPlanned));
            }
        }

        var untouched = budget.Expenses.Where(l => !categoryDeltas.ContainsKey(l.Category));
        return budget with { Expenses = untouched.Concat(adjustedLines).ToList() };
    }
}
