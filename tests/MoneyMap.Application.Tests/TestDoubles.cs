using MoneyMap.Application;

namespace MoneyMap.Application.Tests;

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

    public DateTimeOffset UtcNow { get; set; }

    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}

internal sealed class InMemoryBudgetProfileRepository : IBudgetProfileRepository
{
    private BudgetProfile? _stored;
    public int SaveCount { get; private set; }

    public Task<BudgetProfile?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_stored);

    public Task SaveAsync(BudgetProfile profile, CancellationToken cancellationToken = default)
    {
        _stored = profile;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        _stored = null;
        return Task.CompletedTask;
    }
}

/// <summary>A repository whose SaveAsync always fails, to exercise persistence-failure handling.</summary>
internal sealed class ThrowingBudgetProfileRepository : IBudgetProfileRepository
{
    public Task<BudgetProfile?> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult<BudgetProfile?>(null);

    public Task SaveAsync(BudgetProfile profile, CancellationToken cancellationToken = default)
        => throw new IOException("Simulated disk failure.");

    public Task DeleteAsync(CancellationToken cancellationToken = default)
        => throw new IOException("Simulated disk failure.");
}
