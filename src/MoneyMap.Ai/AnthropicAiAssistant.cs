using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyMap.Application.Ai;
using MoneyMap.Domain;

namespace MoneyMap.Ai;

internal sealed record AnthropicMessage([property: JsonPropertyName("role")] string Role, [property: JsonPropertyName("content")] string Content);

internal sealed record AnthropicRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages);

internal sealed record AnthropicContentBlock([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string? Text);

internal sealed record AnthropicResponse([property: JsonPropertyName("content")] List<AnthropicContentBlock>? Content, [property: JsonPropertyName("error")] AnthropicError? Error);

internal sealed record AnthropicError([property: JsonPropertyName("type")] string? Type, [property: JsonPropertyName("message")] string? Message);

// Optional provider that calls the real Anthropic API. The key stays server-side (see
// AnthropicAiAssistantOptions) - it's never sent to the Blazor client.
public sealed class AnthropicAiAssistant : IAiAssistant
{
    private const string SystemPromptTemplate = """
        You are MoneyMap AI, a financial education assistant embedded in a personal budgeting app for
        beginners, everyday money managers, and users who want simplified, accessible explanations.

        Rules you must follow:
        - Use ONLY the JSON financial context provided in the user message for any numbers. Never invent
          or guess a number that isn't in that context.
        - Never state or imply a current market price, interest rate, or statistic you were not given.
          If asked for live data you don't have, say plainly that you don't have access to live data.
        - Never claim an investment outcome is guaranteed. Always describe projections as hypothetical.
        - Never claim to execute a trade, move money, or make a change on the user's behalf.
        - If you want to propose a specific budget change, describe it in plain language AND append one
          fenced block at the very end formatted exactly as:
          ```proposed-changes
          [{"category":"<one of Housing,Utilities,Groceries,SchoolMeals,Transportation,Subscriptions,Debt,Healthcare,Discretionary,Other>","deltaAmount":<number, negative=reduce>,"rationale":"<short reason>"}]
          ```
          Only include this block when you are proposing an actual numeric change. The user must
          explicitly confirm it in the app before anything changes. Never say the change is already made.
        - Keep answers concise, plain-language, and non-judgmental. Do not shame the user about spending.
        - Include a brief disclaimer when discussing investments: projections are hypothetical, not advice.
        """;

    private readonly HttpClient _httpClient;
    private readonly AnthropicAiAssistantOptions _options;
    private readonly ILogger<AnthropicAiAssistant> _logger;

    public AnthropicAiAssistant(HttpClient httpClient, IOptions<AnthropicAiAssistantOptions> options, ILogger<AnthropicAiAssistant> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderName => "Anthropic Claude";

    public bool RequiresNetwork => true;

    public async Task<AiAssistantReply> AskAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.IsConfigured)
        {
            throw new AiProviderUnavailableException(ProviderName);
        }

        var contextJson = JsonSerializer.Serialize(request.Context);
        var messages = new List<AnthropicMessage>();
        foreach (var turn in request.History)
        {
            messages.Add(new AnthropicMessage(turn.Role == ChatRole.User ? "user" : "assistant", turn.Content));
        }

        messages.Add(new AnthropicMessage("user", $"Financial context (JSON): {contextJson}\n\nQuestion: {request.UserMessage}"));

        var payload = new AnthropicRequest(_options.Model, _options.MaxTokens, SystemPromptTemplate, messages);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        HttpResponseMessage httpResponse;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
            {
                Content = JsonContent.Create(payload),
            };
            httpRequest.Headers.Add("x-api-key", _options.ApiKey);
            httpRequest.Headers.Add("anthropic-version", _options.AnthropicVersion);

            httpResponse = await _httpClient.SendAsync(httpRequest, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiProviderTimeoutException(ProviderName);
        }
        catch (HttpRequestException ex)
        {
            throw new AiProviderUnavailableException(ProviderName, ex);
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(httpResponse, timeoutCts.Token).ConfigureAwait(false);
            _logger.LogWarning("Anthropic API returned {StatusCode}: {Body}", (int)httpResponse.StatusCode, body);

            if (httpResponse.StatusCode is System.Net.HttpStatusCode.TooManyRequests or System.Net.HttpStatusCode.ServiceUnavailable)
            {
                throw new AiProviderUnavailableException(ProviderName);
            }

            throw new AiProviderUnavailableException(ProviderName);
        }

        AnthropicResponse? parsed;
        try
        {
            parsed = await httpResponse.Content.ReadFromJsonAsync<AnthropicResponse>(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            throw new AiProviderMalformedResponseException(ProviderName, ex.Message);
        }

        var text = parsed?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new AiProviderMalformedResponseException(ProviderName, "Response contained no text content.");
        }

        var (cleanText, proposedChanges) = ProposedChangeParser.ExtractAndStrip(text, _logger);

        return new AiAssistantReply
        {
            Message = cleanText,
            ProviderName = ProviderName,
            IsFallback = false,
            ProposedChanges = proposedChanges,
        };
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return "<unreadable body>";
        }
    }
}
