namespace MoneyMap.Ai;

// ApiKey should come from an environment variable or user-secrets, not get checked into git.
public sealed class AnthropicAiAssistantOptions
{
    public const string SectionName = "Ai:Anthropic";

    // Empty/null means no remote provider - falls back to the rule-based assistant.
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-sonnet-5";

    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/messages";

    public string AnthropicVersion { get; set; } = "2023-06-01";

    public int TimeoutSeconds { get; set; } = 20;

    public int MaxTokens { get; set; } = 1024;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
