namespace MoneyMap.Domain;

public enum GoalProgressStatus
{
    Completed,
    OnTrack,
    BehindSchedule,

    // Contribution is zero or negative, so this goal never gets there at this rate.
    Stalled,

    // No deadline set, so we can only give a completion estimate, not an on-track/behind verdict.
    NoDeadline,
}

public sealed record SavingsGoalProjection
{
    public required GoalProgressStatus Status { get; init; }

    public required Money MonthlyContribution { get; init; }

    public int? MonthsToCompletion { get; init; }

    public DateOnly? ProjectedCompletionDate { get; init; }

    public decimal ProgressPercent { get; init; }
}

// Contribution needed to hit the deadline, regardless of what's actually being contributed now.
public sealed record RequiredContributionResult
{
    public required bool IsAlreadyComplete { get; init; }

    public required bool HasDeadline { get; init; }

    public bool DeadlineAlreadyPassed { get; init; }

    public int? MonthsUntilDeadline { get; init; }

    public Money? RequiredMonthlyContribution { get; init; }
}

// Straight-line savings projections at a fixed contribution, no investment growth (that's what
// InvestmentSimulator is for).
public static class SavingsGoalCalculator
{
    public static RequiredContributionResult CalculateRequiredMonthlyContribution(SavingsGoal goal, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(goal);

        if (goal.IsComplete)
        {
            return new RequiredContributionResult { IsAlreadyComplete = true, HasDeadline = goal.Deadline.HasValue };
        }

        if (goal.Deadline is not { } deadline)
        {
            return new RequiredContributionResult { IsAlreadyComplete = false, HasDeadline = false };
        }

        if (deadline <= asOf)
        {
            return new RequiredContributionResult { IsAlreadyComplete = false, HasDeadline = true, DeadlineAlreadyPassed = true };
        }

        var months = Math.Max(1, MonthsBetween(asOf, deadline));
        var required = goal.RemainingAmount / months;

        return new RequiredContributionResult
        {
            IsAlreadyComplete = false,
            HasDeadline = true,
            MonthsUntilDeadline = months,
            RequiredMonthlyContribution = required,
        };
    }

    public static SavingsGoalProjection Project(SavingsGoal goal, Money monthlyContribution, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(goal);

        if (monthlyContribution.Currency != goal.TargetAmount.Currency)
        {
            throw new CurrencyMismatchException(monthlyContribution.Currency, goal.TargetAmount.Currency);
        }

        var progressPercent = goal.TargetAmount.IsPositive
            ? Math.Clamp(Math.Round(goal.CurrentAmount.Amount / goal.TargetAmount.Amount * 100m, 1, MidpointRounding.AwayFromZero), 0m, 100m)
            : 0m;

        if (goal.IsComplete)
        {
            return new SavingsGoalProjection
            {
                Status = GoalProgressStatus.Completed,
                MonthlyContribution = monthlyContribution,
                MonthsToCompletion = 0,
                ProjectedCompletionDate = asOf,
                ProgressPercent = 100m,
            };
        }

        if (!monthlyContribution.IsPositive)
        {
            return new SavingsGoalProjection
            {
                Status = GoalProgressStatus.Stalled,
                MonthlyContribution = monthlyContribution,
                ProgressPercent = progressPercent,
            };
        }

        var monthsToCompletion = (int)Math.Ceiling(goal.RemainingAmount.Amount / monthlyContribution.Amount);
        var completionDate = asOf.AddMonths(monthsToCompletion);

        var status = goal.Deadline is { } deadline
            ? (completionDate <= deadline ? GoalProgressStatus.OnTrack : GoalProgressStatus.BehindSchedule)
            : GoalProgressStatus.NoDeadline;

        return new SavingsGoalProjection
        {
            Status = status,
            MonthlyContribution = monthlyContribution,
            MonthsToCompletion = monthsToCompletion,
            ProjectedCompletionDate = completionDate,
            ProgressPercent = progressPercent,
        };
    }

    // Whole calendar months between two dates, ignoring day-of-month.
    private static int MonthsBetween(DateOnly from, DateOnly to)
    {
        return ((to.Year - from.Year) * 12) + (to.Month - from.Month);
    }
}
