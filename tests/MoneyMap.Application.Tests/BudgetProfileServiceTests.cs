using MoneyMap.Application.Ai;
using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Application.Tests;

public class BudgetProfileServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static BudgetProfileService CreateService(out InMemoryBudgetProfileRepository repo)
    {
        repo = new InMemoryBudgetProfileRepository();
        return new BudgetProfileService(repo, new FixedClock(Now));
    }

    [Fact]
    public async Task GetProfileAsync_WhenNothingStored_ReturnsBlankProfile()
    {
        var service = CreateService(out _);

        var profile = await service.GetProfileAsync();

        Assert.Empty(profile.Budget.IncomeSources);
        Assert.False(profile.IsSampleData);
    }

    [Fact]
    public async Task AddIncomeSourceAsync_PersistsAndReturnsUpdatedProfile()
    {
        var service = CreateService(out var repo);

        var profile = await service.AddIncomeSourceAsync("Job", 2000m, IncomeFrequency.Monthly, grossAmount: 2400m);

        Assert.Single(profile.Budget.IncomeSources);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task AddIncomeSourceAsync_WithInvalidAmount_ThrowsAndDoesNotPersist()
    {
        var service = CreateService(out var repo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.AddIncomeSourceAsync("Job", -100m, IncomeFrequency.Monthly, null));
        Assert.Equal(0, repo.SaveCount);
    }

    [Fact]
    public async Task RemoveExpenseLineAsync_RemovesOnlyMatchingLine()
    {
        var service = CreateService(out _);
        var profile = await service.AddExpenseLineAsync("Rent", ExpenseCategory.Housing, 500m, null, null);
        profile = await service.AddExpenseLineAsync("Groceries", ExpenseCategory.Groceries, 200m, null, null);
        var rentId = profile.Budget.Expenses.First(e => e.Name == "Rent").Id;

        var updated = await service.RemoveExpenseLineAsync(rentId);

        Assert.Single(updated.Budget.Expenses);
        Assert.Equal("Groceries", updated.Budget.Expenses[0].Name);
    }

    [Fact]
    public async Task LoadSampleDataAsync_FlagsProfileAsSample_AndSubsequentEditClearsFlag()
    {
        var service = CreateService(out _);

        var sample = await service.LoadSampleDataAsync();
        Assert.True(sample.IsSampleData);

        var edited = await service.AddExpenseLineAsync("Extra", ExpenseCategory.Other, 10m, null, null);
        Assert.False(edited.IsSampleData);
    }

    [Fact]
    public async Task DeleteAllDataAsync_ClearsStoredProfile()
    {
        var service = CreateService(out var repo);
        await service.AddIncomeSourceAsync("Job", 1000m, IncomeFrequency.Monthly, null);

        await service.DeleteAllDataAsync();
        var stored = await repo.LoadAsync();

        Assert.Null(stored);
    }

    [Fact]
    public async Task ApplyProposedChangeAsync_AdjustsCategoryPlannedAmount()
    {
        var service = CreateService(out _);
        await service.AddExpenseLineAsync("Restaurants", ExpenseCategory.Discretionary, 200m, null, null);

        var change = new ProposedBudgetChange { Category = ExpenseCategory.Discretionary, DeltaAmount = -50m, Rationale = "test" };
        var updated = await service.ApplyProposedChangeAsync(change);

        Assert.Equal(150m, updated.Budget.Expenses.Single().PlannedAmount.Amount);
    }

    [Fact]
    public async Task ReplaceIncomeSourceAsync_WhenIdNotFound_Throws()
    {
        var service = CreateService(out _);
        var bogus = IncomeSource.Create("Ghost", 100m, "USD", IncomeFrequency.Monthly);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplaceIncomeSourceAsync(bogus));
    }

    [Fact]
    public async Task SaveFailure_DoesNotCorruptInMemoryCache()
    {
        var service = new BudgetProfileService(new ThrowingBudgetProfileRepository(), new FixedClock(Now));

        await Assert.ThrowsAsync<IOException>(() => service.AddIncomeSourceAsync("Job", 1000m, IncomeFrequency.Monthly, null));

        // A subsequent read should not throw or return a half-written profile.
        var profile = await service.GetProfileAsync();
        Assert.Empty(profile.Budget.IncomeSources);
    }

    [Fact]
    public async Task GetSummaryAsync_ReflectsSameStateAsGetInsightsAsync()
    {
        var service = CreateService(out _);
        await service.AddIncomeSourceAsync("Job", 1000m, IncomeFrequency.Monthly, null);
        await service.AddExpenseLineAsync("Rent", ExpenseCategory.Housing, 1500m, null, null);

        var summary = await service.GetSummaryAsync();
        var insights = await service.GetInsightsAsync();

        Assert.True(summary.PlannedExceedsIncome);
        Assert.Contains(insights, i => i.Key == "planned-exceeds-income");
    }
}
