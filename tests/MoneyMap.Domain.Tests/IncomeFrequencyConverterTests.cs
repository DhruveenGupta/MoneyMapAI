using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class IncomeFrequencyConverterTests
{
    [Theory]
    [InlineData(IncomeFrequency.Weekly, 100, 433.33)]
    [InlineData(IncomeFrequency.Biweekly, 500, 1083.33)]
    [InlineData(IncomeFrequency.SemiMonthly, 1000, 2000.00)]
    [InlineData(IncomeFrequency.Monthly, 3000, 3000.00)]
    [InlineData(IncomeFrequency.Annually, 60000, 5000.00)]
    public void ToMonthly_NormalizesAccordingToDocumentedRules(IncomeFrequency frequency, decimal amount, decimal expectedMonthly)
    {
        var money = new Money(amount, "USD");

        var monthly = IncomeFrequencyConverter.ToMonthly(money, frequency);

        Assert.Equal(expectedMonthly, monthly.Amount);
    }

    [Theory]
    [InlineData(IncomeFrequency.Weekly, 100, 5200.00)]
    [InlineData(IncomeFrequency.Biweekly, 500, 13000.00)]
    [InlineData(IncomeFrequency.SemiMonthly, 1000, 24000.00)]
    [InlineData(IncomeFrequency.Monthly, 3000, 36000.00)]
    [InlineData(IncomeFrequency.Annually, 60000, 60000.00)]
    public void ToAnnual_NormalizesAccordingToDocumentedRules(IncomeFrequency frequency, decimal amount, decimal expectedAnnual)
    {
        var money = new Money(amount, "USD");

        var annual = IncomeFrequencyConverter.ToAnnual(money, frequency);

        Assert.Equal(expectedAnnual, annual.Amount);
    }

    [Fact]
    public void ZeroIncome_NormalizesToZeroAtAnyFrequency()
    {
        var zero = Money.Zero("USD");

        foreach (IncomeFrequency frequency in Enum.GetValues<IncomeFrequency>())
        {
            Assert.Equal(0m, IncomeFrequencyConverter.ToMonthly(zero, frequency).Amount);
        }
    }
}
