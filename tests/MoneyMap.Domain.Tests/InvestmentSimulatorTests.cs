using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class InvestmentSimulatorTests
{
    private const string Currency = "USD";

    private static InvestmentAssumptions BaseAssumptions(decimal initial = 1000m, decimal monthly = 100m, int years = 1, decimal returnPercent = 0m, decimal? inflation = null, decimal? fee = null) => new()
    {
        InitialInvestment = new Money(initial, Currency),
        MonthlyContribution = new Money(monthly, Currency),
        YearsHorizon = years,
        AnnualReturnPercent = returnPercent,
        AnnualInflationPercent = inflation,
        AnnualFeePercent = fee,
    };

    [Fact]
    public void Simulate_WithZeroReturn_EndingBalanceEqualsContributions()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 100m, years: 1, returnPercent: 0m));

        Assert.Equal(2200m, result.TotalContributions.Amount);
        Assert.Equal(2200m, result.ProjectedEndingBalance.Amount);
        Assert.Equal(0m, result.HypotheticalGrowth.Amount);
    }

    [Fact]
    public void Simulate_WithPositiveReturn_GrowthIsPositive()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 100m, years: 10, returnPercent: 7m));

        Assert.True(result.ProjectedEndingBalance.Amount > result.TotalContributions.Amount);
        Assert.True(result.HypotheticalGrowth.IsPositive);
    }

    [Fact]
    public void Simulate_WithNegativeReturn_EndingBalanceBelowContributions()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 0m, years: 5, returnPercent: -10m));

        Assert.True(result.ProjectedEndingBalance.Amount < result.TotalContributions.Amount);
    }

    [Fact]
    public void Simulate_HypotheticalGrowth_AlwaysEqualsEndingMinusContributions()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(initial: 500m, monthly: 50m, years: 3, returnPercent: 5m));

        Assert.Equal(result.ProjectedEndingBalance.Amount - result.TotalContributions.Amount, result.HypotheticalGrowth.Amount);
    }

    [Fact]
    public void Simulate_WithFees_ProducesLowerBalanceThanWithoutFees()
    {
        var withoutFee = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 100m, years: 10, returnPercent: 7m, fee: 0m));
        var withFee = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 100m, years: 10, returnPercent: 7m, fee: 1m));

        Assert.True(withFee.ProjectedEndingBalance.Amount < withoutFee.ProjectedEndingBalance.Amount);
    }

    [Fact]
    public void Simulate_WithInflation_InflationAdjustedBalanceIsLowerThanNominal()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(initial: 1000m, monthly: 100m, years: 10, returnPercent: 7m, inflation: 3m));

        Assert.NotNull(result.InflationAdjustedEndingBalance);
        Assert.True(result.InflationAdjustedEndingBalance!.Value.Amount < result.ProjectedEndingBalance.Amount);
    }

    [Fact]
    public void Simulate_WithoutInflationAssumption_InflationAdjustedBalanceIsNull()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(inflation: null));

        Assert.Null(result.InflationAdjustedEndingBalance);
    }

    [Fact]
    public void Simulate_YearlySeries_HasOneEntryPerYearPlusYearZero()
    {
        var result = InvestmentSimulator.Simulate(BaseAssumptions(years: 5));

        Assert.Equal(6, result.YearlySeries.Count);
        Assert.Equal(0, result.YearlySeries[0].Year);
        Assert.Equal(5, result.YearlySeries[^1].Year);
    }

    [Theory]
    [InlineData(-1, 100, 1, 5)]
    [InlineData(100, -1, 1, 5)]
    public void Simulate_RejectsNegativeInitialOrContribution(decimal initial, decimal monthly, int years, decimal returnPercent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(initial, monthly, years, returnPercent)));
    }

    [Fact]
    public void Simulate_RejectsZeroOrNegativeHorizon()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(years: 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(years: -1)));
    }

    [Fact]
    public void Simulate_RejectsReturnBelowNegativeOneHundredPercent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(returnPercent: -150m)));
    }

    [Fact]
    public void Simulate_RejectsInflationBelowNegativeOneHundredPercent()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(inflation: -150m)));
    }

    [Fact]
    public void Simulate_RejectsNegativeFee()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InvestmentSimulator.Simulate(BaseAssumptions(fee: -1m)));
    }
}
