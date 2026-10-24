using MoneyMap.Application.Ai;
using MoneyMap.Domain;

namespace MoneyMap.Application;

/// <summary>
/// The single application-layer entry point for reading and mutating the user's financial data.
/// Every page, chart, and AI context in the Web project goes through this service, so they can
/// never disagree about what the current budget is. All validation is delegated to the Domain
/// layer's factory methods, which throw <see cref="ArgumentException"/>-family exceptions on
/// invalid input; callers (the UI) are expected to catch those and show inline validation errors.
/// </summary>
public sealed class BudgetProfileService
{
    private readonly IBudgetProfileRepository _repository;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _mutationLock = new(1, 1);
    private BudgetProfile? _cached;

    public const string DefaultCurrency = "USD";

    public BudgetProfileService(IBudgetProfileRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<BudgetProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var loaded = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);
        _cached = loaded ?? BudgetProfile.CreateBlank(DefaultCurrency);
        return _cached;
    }

    public async Task<BudgetSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
        return BudgetCalculator.Summarize(profile.Budget);
    }

    public async Task<IReadOnlyList<Insight>> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
        var summary = BudgetCalculator.Summarize(profile.Budget);
        return InsightsEngine.Generate(profile.Budget, summary, profile.SavingsGoals, _clock.Today);
    }

    public Task<BudgetSummary> SimulateCategoryChangeAsync(IReadOnlyDictionary<ExpenseCategory, decimal> deltas, CancellationToken cancellationToken = default)
        => GetProfileAsync(cancellationToken).ContinueWith(
            t => BudgetCalculator.SimulateCategoryChange(t.Result.Budget, deltas),
            cancellationToken,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);

    public async Task<BudgetProfile> LoadSampleDataAsync(CancellationToken cancellationToken = default)
    {
        var sample = SampleDataFactory.CreateSampleProfile(_clock);
        await PersistAsync(sample, cancellationToken).ConfigureAwait(false);
        return sample;
    }

    public async Task<BudgetProfile> ResetToBlankAsync(string currency, CancellationToken cancellationToken = default)
    {
        var blank = BudgetProfile.CreateBlank(currency);
        await PersistAsync(blank, cancellationToken).ConfigureAwait(false);
        return blank;
    }

    public async Task DeleteAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _repository.DeleteAsync(cancellationToken).ConfigureAwait(false);
            _cached = null;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    public Task<BudgetProfile> AddIncomeSourceAsync(string name, decimal netAmount, IncomeFrequency frequency, decimal? grossAmount, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var source = IncomeSource.Create(name, netAmount, profile.Budget.Currency, frequency, grossAmount);
            return profile with { Budget = profile.Budget with { IncomeSources = Append(profile.Budget.IncomeSources, source) } };
        }, cancellationToken);

    public Task<BudgetProfile> ReplaceIncomeSourceAsync(IncomeSource updated, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var replaced = Replace(profile.Budget.IncomeSources, updated, s => s.Id);
            return profile with { Budget = profile.Budget with { IncomeSources = replaced } };
        }, cancellationToken);

    public Task<BudgetProfile> RemoveIncomeSourceAsync(Guid id, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var remaining = profile.Budget.IncomeSources.Where(s => s.Id != id).ToList();
            return profile with { Budget = profile.Budget with { IncomeSources = remaining } };
        }, cancellationToken);

    public Task<BudgetProfile> AddExpenseLineAsync(string name, ExpenseCategory category, decimal plannedAmount, decimal? actualAmount, string? notes, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var line = BudgetLine.Create(name, category, plannedAmount, profile.Budget.Currency, actualAmount, notes);
            return profile with { Budget = profile.Budget with { Expenses = Append(profile.Budget.Expenses, line) } };
        }, cancellationToken);

    public Task<BudgetProfile> ReplaceExpenseLineAsync(BudgetLine updated, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var replaced = Replace(profile.Budget.Expenses, updated, l => l.Id);
            return profile with { Budget = profile.Budget with { Expenses = replaced } };
        }, cancellationToken);

    public Task<BudgetProfile> RemoveExpenseLineAsync(Guid id, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var remaining = profile.Budget.Expenses.Where(l => l.Id != id).ToList();
            return profile with { Budget = profile.Budget with { Expenses = remaining } };
        }, cancellationToken);

    public Task<BudgetProfile> AddSavingsGoalAsync(string name, decimal targetAmount, decimal currentAmount, DateOnly? deadline, decimal? plannedMonthlyContribution, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var goal = SavingsGoal.Create(name, targetAmount, currentAmount, profile.Budget.Currency, deadline, _clock.Today, plannedMonthlyContribution);
            return profile with { SavingsGoals = Append(profile.SavingsGoals, goal) };
        }, cancellationToken);

    public Task<BudgetProfile> ReplaceSavingsGoalAsync(SavingsGoal updated, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var replaced = Replace(profile.SavingsGoals, updated, g => g.Id);
            return profile with { SavingsGoals = replaced };
        }, cancellationToken);

    /// <summary>
    /// Applies an AI-proposed category change to the real budget. Must only be called after the
    /// user has explicitly confirmed the change in the UI — this method performs no confirmation
    /// of its own.
    /// </summary>
    public Task<BudgetProfile> ApplyProposedChangeAsync(ProposedBudgetChange change, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var deltas = new Dictionary<ExpenseCategory, decimal> { [change.Category] = change.DeltaAmount };
            var updatedBudget = BudgetCalculator.ApplyCategoryDeltas(profile.Budget, deltas);
            return profile with { Budget = updatedBudget };
        }, cancellationToken);

    public Task<BudgetProfile> RemoveSavingsGoalAsync(Guid id, CancellationToken cancellationToken = default)
        => MutateAsync(profile =>
        {
            var remaining = profile.SavingsGoals.Where(g => g.Id != id).ToList();
            return profile with { SavingsGoals = remaining };
        }, cancellationToken);

    private async Task<BudgetProfile> MutateAsync(Func<BudgetProfile, BudgetProfile> mutate, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = await GetProfileAsync(cancellationToken).ConfigureAwait(false);
            var updated = mutate(current) with { UpdatedAtUtc = _clock.UtcNow, IsSampleData = false };
            await _repository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            _cached = updated;
            return updated;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private async Task PersistAsync(BudgetProfile profile, CancellationToken cancellationToken)
    {
        await _mutationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);
            _cached = profile;
        }
        finally
        {
            _mutationLock.Release();
        }
    }

    private static IReadOnlyList<T> Append<T>(IReadOnlyList<T> source, T item)
    {
        var list = new List<T>(source) { item };
        return list;
    }

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> source, T updated, Func<T, Guid> idSelector)
    {
        var updatedId = idSelector(updated);
        var list = new List<T>(source.Count);
        var found = false;
        foreach (var item in source)
        {
            if (idSelector(item) == updatedId)
            {
                list.Add(updated);
                found = true;
            }
            else
            {
                list.Add(item);
            }
        }

        if (!found)
        {
            throw new InvalidOperationException($"No existing item with id '{updatedId}' was found to update.");
        }

        return list;
    }
}
