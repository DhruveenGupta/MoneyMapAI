namespace MoneyMap.Domain;

// One budgeted expense. ActualAmount is nullable on purpose - "not recorded" and "recorded as
// zero" aren't the same thing.
public sealed record BudgetLine
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required ExpenseCategory Category { get; init; }

    public required Money PlannedAmount { get; init; }

    public Money? ActualAmount { get; init; }

    public string? Notes { get; init; }

    public bool HasActual => ActualAmount.HasValue;

    public Money? Variance => ActualAmount.HasValue ? ActualAmount.Value - PlannedAmount : null;

    public bool IsOverPlanned => Variance is { IsPositive: true };

    public static BudgetLine Create(string name, ExpenseCategory category, decimal plannedAmount, string currency, decimal? actualAmount = null, string? notes = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Budget line name is required.", nameof(name));
        }

        if (plannedAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedAmount), plannedAmount, "Planned amount cannot be negative.");
        }

        if (actualAmount is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(actualAmount), actualAmount, "Actual amount cannot be negative.");
        }

        return new BudgetLine
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Category = category,
            PlannedAmount = new Money(plannedAmount, currency),
            ActualAmount = actualAmount.HasValue ? new Money(actualAmount.Value, currency) : null,
            Notes = notes,
        };
    }

    public BudgetLine WithPlannedAmount(decimal plannedAmount)
    {
        if (plannedAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedAmount), plannedAmount, "Planned amount cannot be negative.");
        }

        return this with { PlannedAmount = new Money(plannedAmount, PlannedAmount.Currency) };
    }

    public BudgetLine WithActualAmount(decimal? actualAmount)
    {
        if (actualAmount is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(actualAmount), actualAmount, "Actual amount cannot be negative.");
        }

        return this with { ActualAmount = actualAmount.HasValue ? new Money(actualAmount.Value, PlannedAmount.Currency) : null };
    }
}
