namespace MoneyMap.Domain;

public sealed record SavingsGoal
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required Money TargetAmount { get; init; }

    public required Money CurrentAmount { get; init; }

    public DateOnly? Deadline { get; init; }

    /// <summary>What the user currently intends to contribute per month, if they've decided. Optional.</summary>
    public Money? PlannedMonthlyContribution { get; init; }

    public required DateOnly CreatedOn { get; init; }

    public bool IsComplete => CurrentAmount >= TargetAmount;

    public Money RemainingAmount => CurrentAmount >= TargetAmount
        ? Money.Zero(TargetAmount.Currency)
        : TargetAmount - CurrentAmount;

    public static SavingsGoal Create(string name, decimal targetAmount, decimal currentAmount, string currency, DateOnly? deadline, DateOnly createdOn, decimal? plannedMonthlyContribution = null, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Goal name is required.", nameof(name));
        }

        if (targetAmount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(targetAmount), targetAmount, "Target amount must be greater than zero.");
        }

        if (currentAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(currentAmount), currentAmount, "Current amount cannot be negative.");
        }

        if (deadline.HasValue && deadline.Value < createdOn)
        {
            throw new ArgumentOutOfRangeException(nameof(deadline), deadline, "Deadline cannot be before today.");
        }

        if (plannedMonthlyContribution is < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedMonthlyContribution), plannedMonthlyContribution, "Planned contribution cannot be negative.");
        }

        return new SavingsGoal
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            TargetAmount = new Money(targetAmount, currency),
            CurrentAmount = new Money(currentAmount, currency),
            Deadline = deadline,
            PlannedMonthlyContribution = plannedMonthlyContribution.HasValue ? new Money(plannedMonthlyContribution.Value, currency) : null,
            CreatedOn = createdOn,
        };
    }
}
