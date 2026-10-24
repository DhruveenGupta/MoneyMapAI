namespace MoneyMap.Domain;

/// <summary>How often an income source is paid.</summary>
public enum IncomeFrequency
{
    Weekly,
    Biweekly,
    SemiMonthly,
    Monthly,
    Annually,
}

/// <summary>
/// Documented, deterministic rules for normalizing income of any frequency into a common monthly
/// or annual basis. These rules use the standard 52-week / 26-biweekly-period calendar year
/// convention, not a flat "weeks * 4" approximation, so monthly figures reconcile with annual ones.
/// </summary>
public static class IncomeFrequencyConverter
{
    private const decimal WeeksPerYear = 52m;
    private const decimal BiweeklyPeriodsPerYear = 26m;
    private const decimal SemiMonthlyPeriodsPerYear = 24m;
    private const decimal MonthsPerYear = 12m;

    /// <summary>Converts a single payment amount at the given frequency into its monthly equivalent.</summary>
    public static Money ToMonthly(Money amount, IncomeFrequency frequency) => frequency switch
    {
        IncomeFrequency.Weekly => amount * WeeksPerYear / MonthsPerYear,
        IncomeFrequency.Biweekly => amount * BiweeklyPeriodsPerYear / MonthsPerYear,
        IncomeFrequency.SemiMonthly => amount * 2m,
        IncomeFrequency.Monthly => amount,
        IncomeFrequency.Annually => amount / MonthsPerYear,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported income frequency."),
    };

    /// <summary>Converts a single payment amount at the given frequency into its annual equivalent.</summary>
    public static Money ToAnnual(Money amount, IncomeFrequency frequency) => frequency switch
    {
        IncomeFrequency.Weekly => amount * WeeksPerYear,
        IncomeFrequency.Biweekly => amount * BiweeklyPeriodsPerYear,
        IncomeFrequency.SemiMonthly => amount * SemiMonthlyPeriodsPerYear,
        IncomeFrequency.Monthly => amount * MonthsPerYear,
        IncomeFrequency.Annually => amount,
        _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported income frequency."),
    };

    public static string Describe(IncomeFrequency frequency) => frequency switch
    {
        IncomeFrequency.Weekly => "Weekly (52 pay periods/year)",
        IncomeFrequency.Biweekly => "Every 2 weeks (26 pay periods/year)",
        IncomeFrequency.SemiMonthly => "Twice a month (24 pay periods/year)",
        IncomeFrequency.Monthly => "Monthly (12 pay periods/year)",
        IncomeFrequency.Annually => "Annually (1 pay period/year)",
        _ => frequency.ToString(),
    };
}
