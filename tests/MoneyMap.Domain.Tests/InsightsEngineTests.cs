using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class InsightsEngineTests
{
    private const string Currency = "USD";
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Fact]
    public void Generate_WithNoIncome_IncludesMissingIncomeInsight()
    {
        var budget = Budget.Empty(Currency);
        var summary = BudgetCalculator.Summarize(budget);

        var insights = InsightsEngine.Generate(budget, summary, Array.Empty<SavingsGoal>(), Today);

        Assert.Contains(insights, i => i.Key == "missing-income");
    }

    [Fact]
    public void Generate_WhenPlannedExceedsIncome_IncludesCriticalInsight()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 1000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 1500m, Currency) },
        };
        var summary = BudgetCalculator.Summarize(budget);

        var insights = InsightsEngine.Generate(budget, summary, Array.Empty<SavingsGoal>(), Today);

        var insight = Assert.Single(insights, i => i.Key == "planned-exceeds-income");
        Assert.Equal(InsightSeverity.Critical, insight.Severity);
        Assert.Contains("500.00", insight.Message);
    }

    [Fact]
    public void Generate_WithOverBudgetLine_IncludesWarning()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 3000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Groceries", ExpenseCategory.Groceries, 300m, Currency, actualAmount: 350m) },
        };
        var summary = BudgetCalculator.Summarize(budget);

        var insights = InsightsEngine.Generate(budget, summary, Array.Empty<SavingsGoal>(), Today);

        Assert.Contains(insights, i => i.Severity == InsightSeverity.Warning && i.Title.Contains("Groceries"));
    }

    [Fact]
    public void Generate_ForCompletedGoal_IncludesPositiveInsight()
    {
        var budget = Budget.Empty(Currency);
        var summary = BudgetCalculator.Summarize(budget);
        var goal = SavingsGoal.Create("Bike", 100m, 100m, Currency, null, Today);

        var insights = InsightsEngine.Generate(budget, summary, new[] { goal }, Today);

        var insight = Assert.Single(insights, i => i.Key == $"goal-{goal.Id}");
        Assert.Equal(InsightSeverity.Positive, insight.Severity);
    }

    [Fact]
    public void Generate_ForGoalBehindSchedule_IncludesWarning()
    {
        var budget = Budget.Empty(Currency);
        var summary = BudgetCalculator.Summarize(budget);
        var goal = SavingsGoal.Create("Trip", 1200m, 0m, Currency, new DateOnly(2026, 3, 1), Today, plannedMonthlyContribution: 50m);

        var insights = InsightsEngine.Generate(budget, summary, new[] { goal }, Today);

        var insight = Assert.Single(insights, i => i.Key == $"goal-{goal.Id}");
        Assert.Equal(InsightSeverity.Warning, insight.Severity);
    }

    [Fact]
    public void Generate_IsDeterministic_SameInputsProduceSameOutput()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 3000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 1000m, Currency) },
        };
        var summary = BudgetCalculator.Summarize(budget);

        var first = InsightsEngine.Generate(budget, summary, Array.Empty<SavingsGoal>(), Today);
        var second = InsightsEngine.Generate(budget, summary, Array.Empty<SavingsGoal>(), Today);

        Assert.Equal(first.Select(i => i.Key), second.Select(i => i.Key));
        Assert.Equal(first.Select(i => i.Message), second.Select(i => i.Message));
    }
}
