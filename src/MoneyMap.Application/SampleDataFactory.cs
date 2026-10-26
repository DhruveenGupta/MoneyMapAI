using MoneyMap.Domain;

namespace MoneyMap.Application;

// Builds the sample budget shown when someone clicks "start with sample data" on the home page.
public static class SampleDataFactory
{
    public const string SampleCurrency = "USD";

    public static BudgetProfile CreateSampleProfile(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var today = clock.Today;
        var now = clock.UtcNow;

        var income = new[]
        {
            IncomeSource.Create("Part-time job", 650m, SampleCurrency, IncomeFrequency.Biweekly, grossAmount: 720m),
        };

        var expenses = new[]
        {
            BudgetLine.Create("Rent share", ExpenseCategory.Housing, 450m, SampleCurrency, actualAmount: 450m),
            BudgetLine.Create("Electric & internet", ExpenseCategory.Utilities, 90m, SampleCurrency, actualAmount: 102m),
            BudgetLine.Create("Groceries", ExpenseCategory.Groceries, 220m, SampleCurrency, actualAmount: 245m),
            BudgetLine.Create("School meal plan", ExpenseCategory.SchoolMeals, 60m, SampleCurrency, actualAmount: 60m),
            BudgetLine.Create("Bus pass", ExpenseCategory.Transportation, 45m, SampleCurrency, actualAmount: 45m),
            BudgetLine.Create("Streaming subscriptions", ExpenseCategory.Subscriptions, 25m, SampleCurrency, actualAmount: 32m),
            BudgetLine.Create("Student loan payment", ExpenseCategory.Debt, 75m, SampleCurrency, actualAmount: 75m),
            BudgetLine.Create("Eating out & fun", ExpenseCategory.Discretionary, 150m, SampleCurrency, actualAmount: 188m),
        };

        var goals = new[]
        {
            SavingsGoal.Create("Emergency fund", 1000m, 250m, SampleCurrency, today.AddMonths(10), today, plannedMonthlyContribution: 75m),
            SavingsGoal.Create("New laptop", 900m, 180m, SampleCurrency, today.AddMonths(6), today, plannedMonthlyContribution: 60m),
        };

        var budget = new Budget
        {
            Currency = SampleCurrency,
            IncomeSources = income,
            Expenses = expenses,
            CurrentSavings = new Money(430m, SampleCurrency),
            EmergencyFundTarget = new Money(1000m, SampleCurrency),
        };

        return new BudgetProfile
        {
            Id = Guid.NewGuid(),
            Budget = budget,
            SavingsGoals = goals,
            IsSampleData = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
