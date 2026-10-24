namespace MoneyMap.Domain;

public sealed record InvestmentYearPoint
{
    public required int Year { get; init; }

    public required Money ContributionsToDate { get; init; }

    public required Money NominalBalance { get; init; }

    public Money? InflationAdjustedBalance { get; init; }
}

/// <summary>
/// Result of running the hypothetical investment simulator. Every figure is clearly a projection
/// of the <see cref="InvestmentAssumptions"/> supplied, never a guarantee or a market fact.
/// </summary>
public sealed record InvestmentProjection
{
    public required InvestmentAssumptions Assumptions { get; init; }

    public required Money TotalContributions { get; init; }

    public required Money ProjectedEndingBalance { get; init; }

    public Money? InflationAdjustedEndingBalance { get; init; }

    public required Money HypotheticalGrowth { get; init; }

    public required IReadOnlyList<InvestmentYearPoint> YearlySeries { get; init; }
}

/// <summary>
/// Educational, hypothetical compounding simulator. Uses monthly compounding of
/// (annual return - annual fee) with contributions added at the end of each month. This is a
/// simplified model appropriate for financial education, not a brokerage-grade projection tool.
/// </summary>
public static class InvestmentSimulator
{
    public static InvestmentProjection Simulate(InvestmentAssumptions assumptions)
    {
        assumptions = InvestmentAssumptions.Validate(assumptions);
        var currency = assumptions.InitialInvestment.Currency;

        var netAnnualRate = (assumptions.AnnualReturnPercent - (assumptions.AnnualFeePercent ?? 0m)) / 100m;
        var monthlyRate = netAnnualRate / 12m;
        var totalMonths = assumptions.YearsHorizon * 12;

        var balance = assumptions.InitialInvestment.Amount;
        var contributions = assumptions.InitialInvestment.Amount;
        var series = new List<InvestmentYearPoint>(assumptions.YearsHorizon + 1)
        {
            new()
            {
                Year = 0,
                ContributionsToDate = new Money(contributions, currency),
                NominalBalance = new Money(balance, currency),
                InflationAdjustedBalance = assumptions.AnnualInflationPercent.HasValue
                    ? new Money(balance, currency)
                    : null,
            },
        };

        for (var month = 1; month <= totalMonths; month++)
        {
            balance = (balance * (1m + monthlyRate)) + assumptions.MonthlyContribution.Amount;
            contributions += assumptions.MonthlyContribution.Amount;

            if (month % 12 == 0)
            {
                var year = month / 12;
                Money? inflationAdjusted = null;
                if (assumptions.AnnualInflationPercent is { } inflation)
                {
                    var deflator = (double)Math.Pow(1.0 + (double)(inflation / 100m), year);
                    inflationAdjusted = new Money((decimal)((double)balance / deflator), currency);
                }

                series.Add(new InvestmentYearPoint
                {
                    Year = year,
                    ContributionsToDate = new Money(contributions, currency),
                    NominalBalance = new Money(balance, currency),
                    InflationAdjustedBalance = inflationAdjusted,
                });
            }
        }

        var endingBalance = new Money(balance, currency);
        var totalContributions = new Money(contributions, currency);

        return new InvestmentProjection
        {
            Assumptions = assumptions,
            TotalContributions = totalContributions,
            ProjectedEndingBalance = endingBalance,
            InflationAdjustedEndingBalance = series[^1].InflationAdjustedBalance,
            HypotheticalGrowth = endingBalance - totalContributions,
            YearlySeries = series,
        };
    }
}
