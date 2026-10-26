namespace MoneyMap.Domain;

// Thrown instead of silently mixing currencies together.
public sealed class CurrencyMismatchException : Exception
{
    public string LeftCurrency { get; }
    public string RightCurrency { get; }

    public CurrencyMismatchException(string leftCurrency, string rightCurrency)
        : base($"Cannot combine amounts in different currencies: '{leftCurrency}' and '{rightCurrency}'.")
    {
        LeftCurrency = leftCurrency;
        RightCurrency = rightCurrency;
    }
}

// Decimal amount + currency code. Rounds to 2 decimal places, round-half-up, like a receipt.
// No floats anywhere.
public readonly record struct Money : IComparable<Money>
{
    public const int MinorUnitDigits = 2;

    public decimal Amount { get; }

    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency code is required.", nameof(currency));
        }

        Currency = currency.Trim().ToUpperInvariant();
        Amount = Math.Round(amount, MinorUnitDigits, MidpointRounding.AwayFromZero);
    }

    public static Money Zero(string currency) => new(0m, currency);

    public bool IsZero => Amount == 0m;

    public bool IsNegative => Amount < 0m;

    public bool IsPositive => Amount > 0m;

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money value) => new(-value.Amount, value.Currency);

    public static Money operator *(Money value, decimal factor) => new(value.Amount * factor, value.Currency);

    public static Money operator *(decimal factor, Money value) => value * factor;

    public static Money operator /(Money value, decimal divisor)
    {
        if (divisor == 0m)
        {
            throw new DivideByZeroException("Cannot divide a monetary amount by zero.");
        }

        return new Money(value.Amount / divisor, value.Currency);
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    // Floors at zero instead of going negative.
    public Money ClampedDifference(Money other)
    {
        EnsureSameCurrency(this, other);
        var result = Amount - other.Amount;
        return result < 0m ? Zero(Currency) : new Money(result, Currency);
    }

    // Null if whole is zero, otherwise a percentage (can go over 100).
    public decimal? PercentOf(Money whole)
    {
        EnsureSameCurrency(this, whole);
        if (whole.Amount == 0m)
        {
            return null;
        }

        return Math.Round(Amount / whole.Amount * 100m, 1, MidpointRounding.AwayFromZero);
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString() => $"{Amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(left.Currency, right.Currency);
        }
    }
}
