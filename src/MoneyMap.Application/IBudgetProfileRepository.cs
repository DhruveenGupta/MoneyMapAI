namespace MoneyMap.Application;

// Storage lives in the Persistence project; this keeps Application from caring how.
public interface IBudgetProfileRepository
{
    Task<BudgetProfile?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BudgetProfile profile, CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}
