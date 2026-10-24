using MoneyMap.Domain;

namespace MoneyMap.Application;

/// <summary>
/// The full persisted state for one user: their budget, their savings goals, and bookkeeping
/// metadata. This is the aggregate the repository loads and saves as a single unit.
/// </summary>
public sealed record BudgetProfile
{
    public required Guid Id { get; init; }

    public required Budget Budget { get; init; }

    public IReadOnlyList<SavingsGoal> SavingsGoals { get; init; } = Array.Empty<SavingsGoal>();

    /// <summary>True when this profile was created from built-in sample data rather than user entry.</summary>
    public bool IsSampleData { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public static BudgetProfile CreateBlank(string currency)
    {
        var now = DateTimeOffset.UtcNow;
        return new BudgetProfile
        {
            Id = Guid.NewGuid(),
            Budget = Budget.Empty(currency),
            IsSampleData = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
