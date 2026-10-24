using MoneyMap.Application.Ai;
using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Application.Tests;

public class AiFinancialContextTests
{
    private const string Currency = "USD";
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Fact]
    public void Build_OmitsFreeTextNotes_IncludesOnlyAggregateFigures()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 3000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 1000m, Currency, notes: "Landlord's private phone number: 555-1234") },
        };
        var summary = BudgetCalculator.Summarize(budget);

        var context = AiFinancialContext.Build(summary, Array.Empty<SavingsGoal>(), Today);

        var serialized = System.Text.Json.JsonSerializer.Serialize(context);
        Assert.DoesNotContain("555-1234", serialized);
        Assert.DoesNotContain("Landlord", serialized);
    }

    [Fact]
    public void Build_IncludesRequiredMonthlyContributionForGoalsWithDeadlines()
    {
        var goal = SavingsGoal.Create("Trip", 1200m, 0m, Currency, new DateOnly(2026, 7, 1), Today);
        var summary = BudgetCalculator.Summarize(Budget.Empty(Currency));

        var context = AiFinancialContext.Build(summary, new[] { goal }, Today);

        Assert.Equal(200m, context.SavingsGoals.Single().RequiredMonthlyContribution);
    }

    [Fact]
    public void Build_WithZeroIncome_LeavesUtilizationNull()
    {
        var summary = BudgetCalculator.Summarize(Budget.Empty(Currency));

        var context = AiFinancialContext.Build(summary, Array.Empty<SavingsGoal>(), Today);

        Assert.Null(context.BudgetUtilizationPercent);
    }
}
