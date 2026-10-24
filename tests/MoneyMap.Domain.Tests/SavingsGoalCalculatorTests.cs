using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Domain.Tests;

public class SavingsGoalCalculatorTests
{
    private const string Currency = "USD";
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Fact]
    public void Project_WhenGoalAlreadyComplete_ReturnsCompleted()
    {
        var goal = SavingsGoal.Create("Emergency Fund", 1000m, 1000m, Currency, null, Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(100m, Currency), Today);

        Assert.Equal(GoalProgressStatus.Completed, projection.Status);
        Assert.Equal(0, projection.MonthsToCompletion);
        Assert.Equal(100m, projection.ProgressPercent);
    }

    [Fact]
    public void Project_WithZeroContribution_IsStalled()
    {
        var goal = SavingsGoal.Create("Laptop", 1000m, 200m, Currency, null, Today);

        var projection = SavingsGoalCalculator.Project(goal, Money.Zero(Currency), Today);

        Assert.Equal(GoalProgressStatus.Stalled, projection.Status);
        Assert.Null(projection.MonthsToCompletion);
    }

    [Fact]
    public void Project_WithNegativeContribution_IsStalled()
    {
        var goal = SavingsGoal.Create("Laptop", 1000m, 200m, Currency, null, Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(-10m, Currency), Today);

        Assert.Equal(GoalProgressStatus.Stalled, projection.Status);
    }

    [Fact]
    public void Project_WithNoDeadline_StillEstimatesCompletion()
    {
        var goal = SavingsGoal.Create("Laptop", 1200m, 0m, Currency, null, Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(100m, Currency), Today);

        Assert.Equal(GoalProgressStatus.NoDeadline, projection.Status);
        Assert.Equal(12, projection.MonthsToCompletion);
        Assert.Equal(new DateOnly(2027, 1, 1), projection.ProjectedCompletionDate);
    }

    [Fact]
    public void Project_WhenContributionMeetsDeadline_IsOnTrack()
    {
        var goal = SavingsGoal.Create("Laptop", 1200m, 0m, Currency, new DateOnly(2027, 1, 1), Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(100m, Currency), Today);

        Assert.Equal(GoalProgressStatus.OnTrack, projection.Status);
    }

    [Fact]
    public void Project_WhenContributionMissesDeadline_IsBehindSchedule()
    {
        var goal = SavingsGoal.Create("Laptop", 1200m, 0m, Currency, new DateOnly(2026, 6, 1), Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(50m, Currency), Today);

        Assert.Equal(GoalProgressStatus.BehindSchedule, projection.Status);
    }

    [Fact]
    public void Project_RoundsUpPartialMonths()
    {
        var goal = SavingsGoal.Create("Bike", 250m, 0m, Currency, null, Today);

        var projection = SavingsGoalCalculator.Project(goal, new Money(100m, Currency), Today);

        // 250 / 100 = 2.5 months -> rounds up to 3
        Assert.Equal(3, projection.MonthsToCompletion);
    }

    [Fact]
    public void CalculateRequiredMonthlyContribution_WhenAlreadyComplete_ReturnsComplete()
    {
        var goal = SavingsGoal.Create("Done", 100m, 100m, Currency, null, Today);

        var result = SavingsGoalCalculator.CalculateRequiredMonthlyContribution(goal, Today);

        Assert.True(result.IsAlreadyComplete);
    }

    [Fact]
    public void CalculateRequiredMonthlyContribution_WithNoDeadline_ReturnsNoDeadline()
    {
        var goal = SavingsGoal.Create("Fund", 1000m, 0m, Currency, null, Today);

        var result = SavingsGoalCalculator.CalculateRequiredMonthlyContribution(goal, Today);

        Assert.False(result.HasDeadline);
        Assert.Null(result.RequiredMonthlyContribution);
    }

    [Fact]
    public void CalculateRequiredMonthlyContribution_WithPassedDeadline_FlagsUnreachable()
    {
        var goal = SavingsGoal.Create("Fund", 1000m, 0m, Currency, new DateOnly(2025, 1, 1), new DateOnly(2024, 1, 1));

        var result = SavingsGoalCalculator.CalculateRequiredMonthlyContribution(goal, Today);

        Assert.True(result.DeadlineAlreadyPassed);
        Assert.Null(result.RequiredMonthlyContribution);
    }

    [Fact]
    public void CalculateRequiredMonthlyContribution_SplitsRemainingAmountEvenlyAcrossMonths()
    {
        var goal = SavingsGoal.Create("Trip", 1200m, 0m, Currency, new DateOnly(2026, 7, 1), Today);

        var result = SavingsGoalCalculator.CalculateRequiredMonthlyContribution(goal, Today);

        Assert.Equal(6, result.MonthsUntilDeadline);
        Assert.Equal(200m, result.RequiredMonthlyContribution!.Value.Amount);
    }

    [Fact]
    public void Create_RejectsZeroOrNegativeTarget()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SavingsGoal.Create("Bad", 0m, 0m, Currency, null, Today));
        Assert.Throws<ArgumentOutOfRangeException>(() => SavingsGoal.Create("Bad", -5m, 0m, Currency, null, Today));
    }

    [Fact]
    public void Create_RejectsDeadlineBeforeToday()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SavingsGoal.Create("Bad", 100m, 0m, Currency, new DateOnly(2020, 1, 1), Today));
    }
}
