namespace MoneyMap.Application;

/// <summary>
/// Persists the single local <see cref="BudgetProfile"/>. Implementations live in the
/// Persistence project so the application layer never depends on a concrete storage technology.
/// </summary>
public interface IBudgetProfileRepository
{
    Task<BudgetProfile?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BudgetProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes all stored financial data for this local profile.</summary>
    Task DeleteAsync(CancellationToken cancellationToken = default);
}
