using Microsoft.Extensions.Logging;
using MoneyMap.Application.Ai;

namespace MoneyMap.Ai;

/// <summary>
/// The single AI entry point the Web layer talks to. Tries a configured remote provider first (if
/// any) and transparently falls back to the always-available rule-based assistant on any provider
/// failure, so a network outage, missing API key, timeout, or malformed response never takes down
/// the AI assistant feature — only degrades it, with that degradation disclosed to the caller via
/// <see cref="AiAssistantReply.IsFallback"/>.
/// </summary>
public sealed class AiAssistantCoordinator
{
    private readonly IAiAssistant? _remoteProvider;
    private readonly RuleBasedAiAssistant _fallbackProvider;
    private readonly ILogger<AiAssistantCoordinator> _logger;

    public AiAssistantCoordinator(RuleBasedAiAssistant fallbackProvider, ILogger<AiAssistantCoordinator> logger, IAiAssistant? remoteProvider = null)
    {
        _fallbackProvider = fallbackProvider;
        _logger = logger;
        _remoteProvider = remoteProvider;
    }

    /// <summary>True when a remote provider is configured and will be attempted before falling back.</summary>
    public bool RemoteProviderConfigured => _remoteProvider is not null;

    public async Task<AiAssistantReply> AskAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (_remoteProvider is null)
        {
            return await _fallbackProvider.AskAsync(request, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await _remoteProvider.AskAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AiProviderException ex)
        {
            _logger.LogWarning(ex, "Remote AI provider {Provider} failed; falling back to built-in assistant.", _remoteProvider.ProviderName);
            var fallback = await _fallbackProvider.AskAsync(request, cancellationToken).ConfigureAwait(false);
            return fallback with
            {
                IsFallback = true,
                LimitationNotice = DescribeFailure(ex),
            };
        }
    }

    private static string DescribeFailure(AiProviderException ex) => ex switch
    {
        AiProviderTimeoutException => "The AI service took too long to respond, so this answer came from MoneyMap's built-in assistant instead.",
        AiProviderMalformedResponseException => "The AI service returned an unexpected response, so this answer came from MoneyMap's built-in assistant instead.",
        _ => "The AI service is temporarily unavailable, so this answer came from MoneyMap's built-in assistant instead.",
    };
}
