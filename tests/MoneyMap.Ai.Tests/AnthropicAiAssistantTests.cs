using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MoneyMap.Application.Ai;
using Xunit;

namespace MoneyMap.Ai.Tests;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return _responder(request, cancellationToken);
    }
}

public class AnthropicAiAssistantTests
{
    private static AnthropicAiAssistant CreateAssistant(StubHttpMessageHandler handler, AnthropicAiAssistantOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        options ??= new AnthropicAiAssistantOptions { ApiKey = "test-key" };
        return new AnthropicAiAssistant(httpClient, Options.Create(options), NullLogger<AnthropicAiAssistant>.Instance);
    }

    [Fact]
    public async Task AskAsync_WithoutApiKey_ThrowsUnavailable()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new InvalidOperationException("should not be called"));
        var assistant = CreateAssistant(handler, new AnthropicAiAssistantOptions { ApiKey = null });

        await Assert.ThrowsAsync<AiProviderUnavailableException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }

    [Fact]
    public async Task AskAsync_OnSuccess_ReturnsParsedTextAndSendsAuthHeaders()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var json = """{"content":[{"type":"text","text":"Here is your answer."}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        });
        var assistant = CreateAssistant(handler);

        var reply = await assistant.AskAsync(TestHelpers.Request("hi"));

        Assert.Equal("Here is your answer.", reply.Message);
        Assert.True(handler.LastRequest!.Headers.Contains("x-api-key"));
        Assert.True(handler.LastRequest!.Headers.Contains("anthropic-version"));
    }

    [Fact]
    public async Task AskAsync_OnSuccess_ExtractsProposedChangesAndStripsThemFromMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var text = "You could cut back a bit.\n```proposed-changes\n[{\"category\":\"Discretionary\",\"deltaAmount\":-25,\"rationale\":\"trim dining out\"}]\n```";
            var json = System.Text.Json.JsonSerializer.Serialize(new { content = new[] { new { type = "text", text } } });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        });
        var assistant = CreateAssistant(handler);

        var reply = await assistant.AskAsync(TestHelpers.Request("How can I save more?"));

        Assert.DoesNotContain("proposed-changes", reply.Message);
        Assert.Single(reply.ProposedChanges);
    }

    [Fact]
    public async Task AskAsync_OnServerError_ThrowsUnavailable()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var assistant = CreateAssistant(handler);

        await Assert.ThrowsAsync<AiProviderUnavailableException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }

    [Fact]
    public async Task AskAsync_OnRateLimit_ThrowsUnavailable()
    {
        var handler = new StubHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var assistant = CreateAssistant(handler);

        await Assert.ThrowsAsync<AiProviderUnavailableException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }

    [Fact]
    public async Task AskAsync_OnTimeout_ThrowsTimeoutException()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var assistant = CreateAssistant(handler, new AnthropicAiAssistantOptions { ApiKey = "k", TimeoutSeconds = 1 });

        await Assert.ThrowsAsync<AiProviderTimeoutException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }

    [Fact]
    public async Task AskAsync_WithEmptyContent_ThrowsMalformedResponse()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            var json = """{"content":[]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") });
        });
        var assistant = CreateAssistant(handler);

        await Assert.ThrowsAsync<AiProviderMalformedResponseException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }

    [Fact]
    public async Task AskAsync_WithInvalidJsonBody_ThrowsMalformedResponse()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not json at all", System.Text.Encoding.UTF8, "application/json") }));
        var assistant = CreateAssistant(handler);

        await Assert.ThrowsAsync<AiProviderMalformedResponseException>(() => assistant.AskAsync(TestHelpers.Request("hi")));
    }
}
