using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class BudgetCalculatorTests
{
    private const string Currency = "USD";

    [Fact]
    public void Summarize_WithZeroIncome_ReportsNullUtilizationAndNoIncome()
    {
        var budget = Budget.Empty(Currency) with
        {
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 500m, Currency) },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.False(summary.HasIncome);
        Assert.Null(summary.BudgetUtilizationPercent);
        Assert.True(summary.PlannedExceedsIncome);
    }

    [Fact]
    public void Summarize_WithMultipleIncomeSources_SumsMonthlyNetAcrossFrequencies()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[]
            {
                IncomeSource.Create("Job", 1000m, Currency, IncomeFrequency.Biweekly),
                IncomeSource.Create("Allowance", 100m, Currency, IncomeFrequency.Monthly),
            },
        };

        var summary = BudgetCalculator.Summarize(budget);

        // 1000 * 26 / 12 = 2166.666... -> 2166.67, plus 100.00 = 2266.67
        Assert.Equal(2266.67m, summary.TotalMonthlyNetIncome.Amount);
    }

    [Fact]
    public void Summarize_IgnoresInactiveIncomeSources()
    {
        var active = IncomeSource.Create("Job", 2000m, Currency, IncomeFrequency.Monthly);
        var inactive = IncomeSource.Create("Old job", 5000m, Currency, IncomeFrequency.Monthly) with { IsActive = false };

        var budget = Budget.Empty(Currency) with { IncomeSources = new[] { active, inactive } };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.Equal(2000m, summary.TotalMonthlyNetIncome.Amount);
    }

    [Fact]
    public void Summarize_WhenExpensesExceedIncome_RemainingFundsIsNegative()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 1000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 1500m, Currency) },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.True(summary.PlannedExceedsIncome);
        Assert.Equal(-500m, summary.PlannedRemainingFunds.Amount);
    }

    [Fact]
    public void Summarize_DistinguishesPlannedFromActual()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 3000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[]
            {
                BudgetLine.Create("Groceries", ExpenseCategory.Groceries, 400m, Currency, actualAmount: 450m),
                BudgetLine.Create("Subscriptions", ExpenseCategory.Subscriptions, 50m, Currency),
            },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.Equal(450m, summary.TotalActualExpenses!.Value.Amount);
        Assert.Equal(450m, summary.OverBudgetLines.Single().ActualAmount!.Value.Amount);
        // Blended = income - (450 actual + 50 planned, since subscriptions has no actual) = 3000 - 500 = 2500
        Assert.Equal(2500m, summary.BlendedRemainingFunds.Amount);
    }

    [Fact]
    public void Summarize_WithNoActualsRecorded_TotalActualIsNull()
    {
        var budget = Budget.Empty(Currency) with
        {
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 500m, Currency) },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.Null(summary.TotalActualExpenses);
        Assert.False(summary.HasAnyActuals);
    }

    [Fact]
    public void Summarize_WithLargeAmounts_ReconcilesExactly()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Salary", 1_000_000.33m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[]
            {
                BudgetLine.Create("A", ExpenseCategory.Other, 333_333.11m, Currency),
                BudgetLine.Create("B", ExpenseCategory.Other, 333_333.11m, Currency),
                BudgetLine.Create("C", ExpenseCategory.Other, 333_333.11m, Currency),
            },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.Equal(999_999.33m, summary.TotalPlannedExpenses.Amount);
        Assert.Equal(1.00m, summary.PlannedRemainingFunds.Amount);
    }

    [Fact]
    public void SimulateCategoryChange_ReducingRestaurantSpending_ShowsExactDifference()
    {
        var budget = Budget.Empty(Currency) with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 3000m, Currency, IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Restaurants", ExpenseCategory.Discretionary, 200m, Currency) },
        };

        var baseline = BudgetCalculator.Summarize(budget);
        var scenario = BudgetCalculator.SimulateCategoryChange(budget, new Dictionary<ExpenseCategory, decimal> { [ExpenseCategory.Discretionary] = -50m });

        Assert.Equal(200m, baseline.TotalPlannedExpenses.Amount);
        Assert.Equal(150m, scenario.TotalPlannedExpenses.Amount);
        Assert.Equal(50m, (scenario.PlannedRemainingFunds - baseline.PlannedRemainingFunds).Amount);
    }

    [Fact]
    public void SimulateCategoryChange_NeverReducesPlannedAmountBelowZero()
    {
        var budget = Budget.Empty(Currency) with
        {
            Expenses = new[] { BudgetLine.Create("Subscriptions", ExpenseCategory.Subscriptions, 20m, Currency) },
        };

        var scenario = BudgetCalculator.SimulateCategoryChange(budget, new Dictionary<ExpenseCategory, decimal> { [ExpenseCategory.Subscriptions] = -100m });

        Assert.Equal(0m, scenario.TotalPlannedExpenses.Amount);
    }

    [Fact]
    public void Summarize_CategoryBreakdown_PercentOfIncomeIsNullWhenIncomeIsZero()
    {
        var budget = Budget.Empty(Currency) with
        {
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 500m, Currency) },
        };

        var summary = BudgetCalculator.Summarize(budget);

        Assert.Null(summary.CategoryBreakdown.Single().PercentOfIncome);
    }
}
