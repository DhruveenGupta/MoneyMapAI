namespace MoneyMap.Domain;

public enum GoalProgressStatus
{
    /// <summary>Current amount already meets or exceeds the target.</summary>
    Completed,

    /// <summary>A contribution is being made and, if a deadline exists, it will be met.</summary>
    OnTrack,

    /// <summary>A deadline exists but the current contribution will not reach it in time.</summary>
    BehindSchedule,

    /// <summary>The contribution is zero (or negative), so the goal will never complete at this rate.</summary>
    Stalled,

    /// <summary>No deadline was set, so "on track" cannot be evaluated — only a completion estimate is given.</summary>
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

/// <summary>Suggested contribution to meet a goal's deadline, computed independently of whatever the user is currently contributing.</summary>
public sealed record RequiredContributionResult
{
    public required bool IsAlreadyComplete { get; init; }

    public required bool HasDeadline { get; init; }

    public bool DeadlineAlreadyPassed { get; init; }

    public int? MonthsUntilDeadline { get; init; }

    public Money? RequiredMonthlyContribution { get; init; }
}

/// <summary>
/// Deterministic savings-goal math. Every projection is a straight-line estimate at a fixed
/// contribution — no investment growth is assumed here (see <see cref="InvestmentSimulator"/> for
/// that) — so the numbers stay easy to audit by hand.
/// </summary>
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

    /// <summary>Whole calendar months between two dates, ignoring day-of-month (a generous, easy-to-explain approximation).</summary>
    private static int MonthsBetween(DateOnly from, DateOnly to)
    {
        return ((to.Year - from.Year) * 12) + (to.Month - from.Month);
    }
}
