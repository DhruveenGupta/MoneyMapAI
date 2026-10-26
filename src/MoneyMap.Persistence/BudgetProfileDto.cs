using MoneyMap.Domain;

namespace MoneyMap.Persistence;

// Plain mutable DTOs for JSON storage, kept separate from the Domain records. Every load goes
// back through the Domain factory methods, which catches a hand-edited or corrupted file.

public sealed class IncomeSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public IncomeFrequency Frequency { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BudgetLineDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExpenseCategory Category { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string? Notes { get; set; }
}

public sealed class SavingsGoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly? Deadline { get; set; }
    public decimal? PlannedMonthlyContribution { get; set; }
    public DateOnly CreatedOn { get; set; }
}

public sealed class BudgetDto
{
    public string Currency { get; set; } = "USD";
    public List<IncomeSourceDto> IncomeSources { get; set; } = new();
    public List<BudgetLineDto> Expenses { get; set; } = new();
    public decimal CurrentSavings { get; set; }
    public decimal? EmergencyFundTarget { get; set; }
}

public sealed class BudgetProfileDto
{
    // Bump this if the on-disk format ever needs to change.
    public int SchemaVersion { get; set; } = 1;
    public Guid Id { get; set; }
    public BudgetDto Budget { get; set; } = new();
    public List<SavingsGoalDto> SavingsGoals { get; set; } = new();
    public bool IsSampleData { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
