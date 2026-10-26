using MoneyMap.Domain;

namespace MoneyMap.Application;

// Everything persisted for one user - budget, goals, and bookkeeping. The repository loads and
// saves this as a single unit.
public sealed record BudgetProfile
{
    public required Guid Id { get; init; }

    public required Budget Budget { get; init; }

    public IReadOnlyList<SavingsGoal> SavingsGoals { get; init; } = Array.Empty<SavingsGoal>();

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
