namespace MoneyMap.Domain;

/// <summary>
/// User-provided assumptions for the educational investment simulator. Every field here is a
/// hypothetical input, not a fact — the simulator must never claim these predict real returns.
/// </summary>
public sealed record InvestmentAssumptions
{
    public required Money InitialInvestment { get; init; }

    public required Money MonthlyContribution { get; init; }

    public required int YearsHorizon { get; init; }

    /// <summary>Assumed average annual return, as a percent (e.g. 7 for 7%). May be zero or negative.</summary>
    public required decimal AnnualReturnPercent { get; init; }

    /// <summary>Optional assumed annual inflation rate, as a percent, used to show a "today's dollars" value.</summary>
    public decimal? AnnualInflationPercent { get; init; }

    /// <summary>Optional annual fee/expense ratio, as a percent, subtracted from the return before compounding.</summary>
    public decimal? AnnualFeePercent { get; init; }

    public static InvestmentAssumptions Validate(InvestmentAssumptions assumptions)
    {
        ArgumentNullException.ThrowIfNull(assumptions);

        if (assumptions.InitialInvestment.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Initial investment cannot be negative.");
        }

        if (assumptions.MonthlyContribution.IsNegative)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Monthly contribution cannot be negative.");
        }

        if (assumptions.YearsHorizon <= 0 || assumptions.YearsHorizon > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Time horizon must be between 1 and 100 years.");
        }

        if (assumptions.AnnualReturnPercent < -100m)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Annual return cannot be less than -100%.");
        }

        if (assumptions.AnnualInflationPercent is < -100m)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Annual inflation cannot be less than -100%.");
        }

        if (assumptions.AnnualFeePercent is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(assumptions), "Annual fee cannot be negative.");
        }

        return assumptions;
    }
}
