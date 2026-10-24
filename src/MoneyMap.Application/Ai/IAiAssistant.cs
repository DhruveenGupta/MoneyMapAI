using MoneyMap.Domain;

namespace MoneyMap.Application.Ai;

public enum ChatRole
{
    User,
    Assistant,
}

public sealed record ChatTurn(ChatRole Role, string Content);

/// <summary>A category-level spending change the assistant suggests. Never applied automatically.</summary>
public sealed record ProposedBudgetChange
{
    public required ExpenseCategory Category { get; init; }

    /// <summary>Change to the category's planned monthly amount; negative reduces spending.</summary>
    public required decimal DeltaAmount { get; init; }

    public required string Rationale { get; init; }
}

public sealed record AiAssistantRequest
{
    public required string UserMessage { get; init; }

    public required AiFinancialContext Context { get; init; }

    public IReadOnlyList<ChatTurn> History { get; init; } = Array.Empty<ChatTurn>();
}

public sealed record AiAssistantReply
{
    public required string Message { get; init; }

    public required string ProviderName { get; init; }

    /// <summary>True when a configured remote provider failed and a safe fallback answered instead.</summary>
    public bool IsFallback { get; init; }

    /// <summary>Shown to the user when the assistant could not do something (e.g. no live market data).</summary>
    public string? LimitationNotice { get; init; }

    public IReadOnlyList<ProposedBudgetChange> ProposedChanges { get; init; } = Array.Empty<ProposedBudgetChange>();
}

/// <summary>
/// Provider abstraction isolating the application from any single LLM vendor. Implementations
/// must never perform or claim to perform any authoritative financial calculation themselves —
/// numbers in a reply must trace back to the <see cref="AiFinancialContext"/> they were given.
/// </summary>
public interface IAiAssistant
{
    string ProviderName { get; }

    /// <summary>Whether this provider requires network access / external credentials to function.</summary>
    bool RequiresNetwork { get; }

    Task<AiAssistantReply> AskAsync(AiAssistantRequest request, CancellationToken cancellationToken = default);
}

public abstract class AiProviderException : Exception
{
    protected AiProviderException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public sealed class AiProviderTimeoutException : AiProviderException
{
    public AiProviderTimeoutException(string provider) : base($"AI provider '{provider}' timed out.")
    {
    }
}

public sealed class AiProviderUnavailableException : AiProviderException
{
    public AiProviderUnavailableException(string provider, Exception? inner = null)
        : base($"AI provider '{provider}' is unavailable.", inner)
    {
    }
}

public sealed class AiProviderMalformedResponseException : AiProviderException
{
    public AiProviderMalformedResponseException(string provider, string details)
        : base($"AI provider '{provider}' returned a response that failed validation: {details}")
    {
    }
}
