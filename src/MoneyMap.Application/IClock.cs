namespace MoneyMap.Application;

/// <summary>Abstracts "now" so application services stay deterministic and testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
