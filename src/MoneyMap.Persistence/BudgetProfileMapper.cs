using MoneyMap.Application;
using MoneyMap.Domain;

namespace MoneyMap.Persistence;

// Converts between the DTO shape and the real domain model.
public static class BudgetProfileMapper
{
    public static BudgetProfileDto ToDto(BudgetProfile profile)
    {
        var currency = profile.Budget.Currency;

        return new BudgetProfileDto
        {
            Id = profile.Id,
            IsSampleData = profile.IsSampleData,
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc,
            Budget = new BudgetDto
            {
                Currency = currency,
                CurrentSavings = profile.Budget.CurrentSavings.Amount,
                EmergencyFundTarget = profile.Budget.EmergencyFundTarget?.Amount,
                IncomeSources = profile.Budget.IncomeSources.Select(i => new IncomeSourceDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    NetAmount = i.NetAmount.Amount,
                    GrossAmount = i.GrossAmount?.Amount,
                    Frequency = i.Frequency,
                    IsActive = i.IsActive,
                }).ToList(),
                Expenses = profile.Budget.Expenses.Select(e => new BudgetLineDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Category = e.Category,
                    PlannedAmount = e.PlannedAmount.Amount,
                    ActualAmount = e.ActualAmount?.Amount,
                    Notes = e.Notes,
                }).ToList(),
            },
            SavingsGoals = profile.SavingsGoals.Select(g => new SavingsGoalDto
            {
                Id = g.Id,
                Name = g.Name,
                TargetAmount = g.TargetAmount.Amount,
                CurrentAmount = g.CurrentAmount.Amount,
                Deadline = g.Deadline,
                PlannedMonthlyContribution = g.PlannedMonthlyContribution?.Amount,
                CreatedOn = g.CreatedOn,
            }).ToList(),
        };
    }

    // Re-runs Domain validation on the way back in - throws on a negative amount, blank name, etc.
    public static BudgetProfile ToDomain(BudgetProfileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var currency = string.IsNullOrWhiteSpace(dto.Budget.Currency) ? "USD" : dto.Budget.Currency;

        var budget = new Budget
        {
            Currency = currency,
            CurrentSavings = new Money(dto.Budget.CurrentSavings, currency),
            EmergencyFundTarget = dto.Budget.EmergencyFundTarget.HasValue ? new Money(dto.Budget.EmergencyFundTarget.Value, currency) : null,
            IncomeSources = dto.Budget.IncomeSources.Select(i =>
                IncomeSource.Create(i.Name, i.NetAmount, currency, i.Frequency, i.GrossAmount, i.IsActive, i.Id)).ToList(),
            Expenses = dto.Budget.Expenses.Select(e =>
                BudgetLine.Create(e.Name, e.Category, e.PlannedAmount, currency, e.ActualAmount, e.Notes, e.Id)).ToList(),
        };

        var goals = dto.SavingsGoals.Select(g =>
            SavingsGoal.Create(g.Name, g.TargetAmount, g.CurrentAmount, currency, g.Deadline, g.CreatedOn, g.PlannedMonthlyContribution, g.Id)).ToList();

        return new BudgetProfile
        {
            Id = dto.Id,
            Budget = budget,
            SavingsGoals = goals,
            IsSampleData = dto.IsSampleData,
            CreatedAtUtc = dto.CreatedAtUtc,
            UpdatedAtUtc = dto.UpdatedAtUtc,
        };
    }
}
