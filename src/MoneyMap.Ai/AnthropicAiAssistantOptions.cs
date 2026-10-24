namespace MoneyMap.Ai;

/// <summary>
/// Configuration for the optional remote AI provider. <see cref="ApiKey"/> must come from
/// server-side configuration (environment variable or user-secrets) — it must never be embedded in
/// client-served code or checked into source control. See .env.example.
/// </summary>
public sealed class AnthropicAiAssistantOptions
{
    public const string SectionName = "Ai:Anthropic";

    /// <summary>Server-side only. When null/empty, the remote provider is disabled and the app uses the rule-based assistant.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5";

    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/messages";

    public string AnthropicVersion { get; set; } = "2023-06-01";

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxTokens { get; set; } = 1024;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
