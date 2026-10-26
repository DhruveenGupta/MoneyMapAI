using Microsoft.Extensions.Logging;
using MoneyMap.Application.Ai;

namespace MoneyMap.Ai;

// Tries the remote provider first if one's configured, falls back to the rule-based assistant
// on any failure (timeout, bad API key, whatever) so a network hiccup doesn't just break the chat.
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
