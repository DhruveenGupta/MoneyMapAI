using System.Globalization;

namespace MoneyMap.Domain;

// Builds insights from plain calculations - same inputs always give the same output, no LLM
// or randomness involved.
public static class InsightsEngine
{
    public static IReadOnlyList<Insight> Generate(Budget budget, BudgetSummary summary, IReadOnlyList<SavingsGoal> goals, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(goals);

        var insights = new List<Insight>();

        if (!summary.HasIncome)
        {
            insights.Add(new Insight
            {
                Key = "missing-income",
                Severity = InsightSeverity.Info,
                Title = "Add an income source",
                Message = "No active monthly income is entered yet, so remaining-funds and budget-utilization figures can't be calculated.",
            });
        }

        if (budget.Expenses.Count == 0)
        {
            insights.Add(new Insight
            {
                Key = "missing-expenses",
                Severity = InsightSeverity.Info,
                Title = "Add your expenses",
                Message = "No budget categories have been entered yet. Add at least one expense to see a budget breakdown.",
            });
        }

        if (summary.HasIncome && summary.PlannedExceedsIncome)
        {
            var over = -summary.PlannedRemainingFunds;
            insights.Add(new Insight
            {
                Key = "planned-exceeds-income",
                Severity = InsightSeverity.Critical,
                Title = "Planned expenses exceed income",
                Message = $"Planned expenses are {FormatMoney(over)} more than monthly take-home income.",
                Assumptions = new[]
                {
                    $"Monthly take-home income: {FormatMoney(summary.TotalMonthlyNetIncome)}.",
                    $"Total planned expenses: {FormatMoney(summary.TotalPlannedExpenses)}.",
                },
            });
        }

        foreach (var line in summary.OverBudgetLines)
        {
            var overAmount = line.Variance!.Value;
            insights.Add(new Insight
            {
                Key = $"over-budget-{line.Id}",
                Severity = InsightSeverity.Warning,
                Title = $"{line.Name} is over its planned amount",
                Message = $"Actual spending on {line.Name} is {FormatMoney(overAmount)} more than the {FormatMoney(line.PlannedAmount)} planned this month.",
                RelatedCategory = line.Category,
            });
        }

        if (summary.HasIncome)
        {
            var largestCategory = summary.CategoryBreakdown.FirstOrDefault();
            if (largestCategory is { PercentOfIncome: { } percent } && percent > 0)
            {
                insights.Add(new Insight
                {
                    Key = "largest-category",
                    Severity = InsightSeverity.Info,
                    Title = $"{ExpenseCategoryInfo.DisplayName(largestCategory.Category)} is the largest planned category",
                    Message = $"{ExpenseCategoryInfo.DisplayName(largestCategory.Category)} represents {percent.ToString("0.#", CultureInfo.InvariantCulture)}% of monthly take-home income ({FormatMoney(largestCategory.PlannedTotal)}).",
                    RelatedCategory = largestCategory.Category,
                });
            }
        }

        if (summary.HasIncome && !summary.PlannedExceedsIncome && summary.PlannedRemainingFunds.IsPositive)
        {
            insights.Add(new Insight
            {
                Key = "remaining-funds-positive",
                Severity = InsightSeverity.Positive,
                Title = "Funds remaining after planned expenses",
                Message = $"After planned expenses, {FormatMoney(summary.PlannedRemainingFunds)} is left each month based on current entries.",
                Assumptions = new[] { "Does not yet account for recorded actuals where they differ from planned amounts." },
            });
        }

        foreach (var goal in goals)
        {
            insights.Add(BuildGoalInsight(goal, asOf));
        }

        return insights;
    }

    private static Insight BuildGoalInsight(SavingsGoal goal, DateOnly asOf)
    {
        if (goal.IsComplete)
        {
            return new Insight
            {
                Key = $"goal-{goal.Id}",
                Severity = InsightSeverity.Positive,
                Title = $"'{goal.Name}' goal reached",
                Message = $"'{goal.Name}' has reached its target of {FormatMoney(goal.TargetAmount)}.",
            };
        }

        if (goal.PlannedMonthlyContribution is { IsPositive: true } contribution)
        {
            var projection = SavingsGoalCalculator.Project(goal, contribution, asOf);
            return projection.Status switch
            {
                GoalProgressStatus.OnTrack or GoalProgressStatus.NoDeadline => new Insight
                {
                    Key = $"goal-{goal.Id}",
                    Severity = InsightSeverity.Positive,
                    Title = $"'{goal.Name}' is on track",
                    Message = $"At {FormatMoney(contribution)}/month, '{goal.Name}' is projected to reach {FormatMoney(goal.TargetAmount)} in about {projection.MonthsToCompletion} month(s), around {projection.ProjectedCompletionDate:MMMM yyyy}.",
                    Assumptions = new[] { "Assumes the current contribution amount stays the same every month and no investment growth." },
                },
                GoalProgressStatus.BehindSchedule => new Insight
                {
                    Key = $"goal-{goal.Id}",
                    Severity = InsightSeverity.Warning,
                    Title = $"'{goal.Name}' may miss its deadline",
                    Message = $"At {FormatMoney(contribution)}/month, '{goal.Name}' is projected to finish around {projection.ProjectedCompletionDate:MMMM yyyy}, after its {goal.Deadline:MMMM yyyy} deadline.",
                    Assumptions = new[] { "Assumes the current contribution amount stays the same every month." },
                },
                _ => new Insight
                {
                    Key = $"goal-{goal.Id}",
                    Severity = InsightSeverity.Info,
                    Title = $"'{goal.Name}' progress",
                    Message = $"'{goal.Name}' is {projection.ProgressPercent.ToString("0.#", CultureInfo.InvariantCulture)}% of the way to {FormatMoney(goal.TargetAmount)}.",
                },
            };
        }

        return new Insight
        {
            Key = $"goal-{goal.Id}",
            Severity = InsightSeverity.Info,
            Title = $"'{goal.Name}' needs a monthly contribution",
            Message = $"'{goal.Name}' has {FormatMoney(goal.RemainingAmount)} left to reach its goal, but no monthly contribution amount has been set yet.",
        };
    }

    private static string FormatMoney(Money money) => money.ToString();
}
