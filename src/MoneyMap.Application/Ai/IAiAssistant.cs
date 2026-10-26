using MoneyMap.Domain;

namespace MoneyMap.Application.Ai;

public enum ChatRole
{
    User,
    Assistant,
}

public sealed record ChatTurn(ChatRole Role, string Content);

// A spending change the assistant suggests. Never applied without the user clicking confirm.
public sealed record ProposedBudgetChange
{
    public required ExpenseCategory Category { get; init; }

    // Negative reduces spending, positive increases it.
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

    // True when a configured remote provider failed and the fallback answered instead.
    public bool IsFallback { get; init; }

    // Shown when the assistant can't do something, e.g. no live market data.
    public string? LimitationNotice { get; init; }

    public IReadOnlyList<ProposedBudgetChange> ProposedChanges { get; init; } = Array.Empty<ProposedBudgetChange>();
}

// Lets the app swap AI providers without caring which one's behind it. Implementations
// shouldn't do their own financial math - numbers in a reply should trace back to the
// AiFinancialContext they were given.
public interface IAiAssistant
{
    string ProviderName { get; }

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
