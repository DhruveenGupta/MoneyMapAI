namespace MoneyMap.Domain;

// Hypothetical inputs for the investment simulator - none of this predicts real returns.
public sealed record InvestmentAssumptions
{
    public required Money InitialInvestment { get; init; }

    public required Money MonthlyContribution { get; init; }

    public required int YearsHorizon { get; init; }

    // Percent, e.g. 7 for 7%. Can be zero or negative.
    public required decimal AnnualReturnPercent { get; init; }

    // Optional, used to show a "today's dollars" value.
    public decimal? AnnualInflationPercent { get; init; }

    // Optional expense ratio, subtracted from the return before compounding.
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
