using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_RoundsToTwoDecimalPlaces_AwayFromZero()
    {
        var money = new Money(10.005m, "usd");

        Assert.Equal(10.01m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Addition_WithSameCurrency_Sums()
    {
        var a = new Money(10.50m, "USD");
        var b = new Money(5.25m, "USD");

        Assert.Equal(new Money(15.75m, "USD"), a + b);
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_Throws()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");

        Assert.Throws<CurrencyMismatchException>(() => a + b);
    }

    [Fact]
    public void Subtraction_LargeAmounts_StaysExact()
    {
        var a = new Money(1_000_000.01m, "USD");
        var b = new Money(999_999.02m, "USD");

        Assert.Equal(new Money(0.99m, "USD"), a - b);
    }

    [Fact]
    public void ClampedDifference_NeverGoesNegative()
    {
        var small = new Money(5m, "USD");
        var large = new Money(20m, "USD");

        Assert.Equal(Money.Zero("USD"), small.ClampedDifference(large));
    }

    [Fact]
    public void PercentOf_WhenWholeIsZero_ReturnsNull()
    {
        var part = new Money(50m, "USD");
        var whole = Money.Zero("USD");

        Assert.Null(part.PercentOf(whole));
    }

    [Fact]
    public void PercentOf_ComputesExpectedPercentage()
    {
        var part = new Money(25m, "USD");
        var whole = new Money(200m, "USD");

        Assert.Equal(12.5m, part.PercentOf(whole));
    }

    [Fact]
    public void Division_ByZero_Throws()
    {
        var a = new Money(10m, "USD");

        Assert.Throws<DivideByZeroException>(() => a / 0m);
    }

    [Fact]
    public void Comparison_WithDifferentCurrencies_Throws()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");

        Assert.Throws<CurrencyMismatchException>(() => a > b);
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData(" eur ", "EUR")]
    public void Currency_IsNormalizedToUppercaseTrimmed(string input, string expected)
    {
        var money = new Money(1m, input);

        Assert.Equal(expected, money.Currency);
    }

    [Fact]
    public void Constructor_RejectsBlankCurrency()
    {
        Assert.Throws<ArgumentException>(() => new Money(1m, ""));
    }
}
