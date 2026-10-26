namespace MoneyMap.Domain;

// A job, allowance, gig, benefit, whatever. Gross amount is optional since plenty of people
// only know their take-home pay.
public sealed record IncomeSource
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required Money NetAmount { get; init; }

    public Money? GrossAmount { get; init; }

    public required IncomeFrequency Frequency { get; init; }

    public bool IsActive { get; init; } = true;

    // Normalized to a monthly amount; zero if this source is turned off.
    public Money MonthlyNet => IsActive ? IncomeFrequencyConverter.ToMonthly(NetAmount, Frequency) : Money.Zero(NetAmount.Currency);

    public static IncomeSource Create(string name, decimal netAmount, string currency, IncomeFrequency frequency, decimal? grossAmount = null, bool isActive = true, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Income source name is required.", nameof(name));
        }

        if (netAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(netAmount), netAmount, "Income cannot be negative.");
        }

        return new IncomeSource
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            NetAmount = new Money(netAmount, currency),
            GrossAmount = grossAmount.HasValue ? new Money(grossAmount.Value, currency) : null,
            Frequency = frequency,
            IsActive = isActive,
        };
    }
}
