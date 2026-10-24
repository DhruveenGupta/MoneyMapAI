using Microsoft.Extensions.Logging.Abstractions;
using MoneyMap.Application;
using MoneyMap.Domain;
using MoneyMap.Persistence;
using Xunit;

namespace MoneyMap.Persistence.Tests;

public class JsonFileBudgetProfileRepositoryTests : IDisposable
{
    private readonly string _dataDirectory;

    public JsonFileBudgetProfileRepositoryTests()
    {
        _dataDirectory = Path.Combine(Path.GetTempPath(), "moneymap-tests-" + Guid.NewGuid());
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }
    }

    private JsonFileBudgetProfileRepository CreateRepository(bool encrypt = true) => new(
        new JsonFileBudgetProfileRepositoryOptions { DataDirectory = _dataDirectory, EnableEncryptionAtRest = encrypt },
        NullLogger<JsonFileBudgetProfileRepository>.Instance);

    private static BudgetProfile SampleProfile()
    {
        var budget = Budget.Empty("USD") with
        {
            IncomeSources = new[] { IncomeSource.Create("Job", 2000m, "USD", IncomeFrequency.Monthly) },
            Expenses = new[] { BudgetLine.Create("Rent", ExpenseCategory.Housing, 800m, "USD", actualAmount: 820m, notes: "Very secret landlord note") },
        };

        return new BudgetProfile
        {
            Id = Guid.NewGuid(),
            Budget = budget,
            SavingsGoals = new[] { SavingsGoal.Create("Emergency fund", 1000m, 200m, "USD", new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1)) },
            IsSampleData = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    [Fact]
    public async Task LoadAsync_WhenNoFileExists_ReturnsNull()
    {
        var repo = CreateRepository();

        var result = await repo.LoadAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsAllDataExactly()
    {
        var repo = CreateRepository();
        var original = SampleProfile();

        await repo.SaveAsync(original);
        var loaded = await repo.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal(original.Id, loaded!.Id);
        Assert.Equal(original.Budget.IncomeSources[0].NetAmount, loaded.Budget.IncomeSources[0].NetAmount);
        Assert.Equal(original.Budget.Expenses[0].Id, loaded.Budget.Expenses[0].Id);
        Assert.Equal(original.Budget.Expenses[0].ActualAmount, loaded.Budget.Expenses[0].ActualAmount);
        Assert.Equal(original.SavingsGoals[0].Deadline, loaded.SavingsGoals[0].Deadline);
    }

    [Fact]
    public async Task SaveAsync_WithEncryptionEnabled_FileDoesNotContainPlaintext()
    {
        var repo = CreateRepository(encrypt: true);
        await repo.SaveAsync(SampleProfile());

        var bytes = await File.ReadAllBytesAsync(Path.Combine(_dataDirectory, "moneymap-profile.dat"));
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.DoesNotContain("secret landlord note", text);
        Assert.DoesNotContain("Rent", text);
    }

    [Fact]
    public async Task SaveAsync_WithEncryptionDisabled_FileIsReadablePlainJson()
    {
        var repo = CreateRepository(encrypt: false);
        await repo.SaveAsync(SampleProfile());

        var bytes = await File.ReadAllBytesAsync(Path.Combine(_dataDirectory, "moneymap-profile.dat"));
        var text = System.Text.Encoding.UTF8.GetString(bytes);

        Assert.Contains("Rent", text);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptFile_QuarantinesItAndReturnsNull()
    {
        Directory.CreateDirectory(_dataDirectory);
        var path = Path.Combine(_dataDirectory, "moneymap-profile.dat");
        await File.WriteAllBytesAsync(path, "not a real profile file"u8.ToArray());
        var repo = CreateRepository();

        var result = await repo.LoadAsync();

        Assert.Null(result);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_dataDirectory, "*.corrupted-*.bak"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesFileAndQuarantinedBackups()
    {
        var repo = CreateRepository();
        await repo.SaveAsync(SampleProfile());

        // Force a quarantine to exist too.
        Directory.CreateDirectory(_dataDirectory);
        File.WriteAllText(Path.Combine(_dataDirectory, "moneymap-profile.dat.corrupted-123.bak"), "x");

        await repo.DeleteAsync();

        Assert.Empty(Directory.GetFiles(_dataDirectory));
    }

    [Fact]
    public async Task SaveAsync_NeverLeavesATempFileBehind()
    {
        var repo = CreateRepository();
        await repo.SaveAsync(SampleProfile());

        Assert.DoesNotContain(Directory.GetFiles(_dataDirectory), f => f.EndsWith(".tmp"));
    }
}
