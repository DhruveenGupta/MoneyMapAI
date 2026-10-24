using Microsoft.Extensions.Logging.Abstractions;
using MoneyMap.Application.Ai;
using Xunit;

namespace MoneyMap.Ai.Tests;

public class AiAssistantCoordinatorTests
{
    private static AiAssistantCoordinator CreateCoordinator(IAiAssistant? remote) =>
        new(new RuleBasedAiAssistant(), NullLogger<AiAssistantCoordinator>.Instance, remote);

    [Fact]
    public async Task AskAsync_WithNoRemoteProvider_UsesFallbackDirectly_NotFlaggedAsFallback()
    {
        var coordinator = CreateCoordinator(remote: null);

        var reply = await coordinator.AskAsync(TestHelpers.Request("explain my budget"));

        Assert.False(reply.IsFallback);
        Assert.Equal("MoneyMap Built-in Assistant", reply.ProviderName);
        Assert.False(coordinator.RemoteProviderConfigured);
    }

    [Fact]
    public async Task AskAsync_WhenRemoteSucceeds_ReturnsRemoteReplyUnchanged()
    {
        var remote = new FakeAiAssistant(_ => new AiAssistantReply { Message = "remote answer", ProviderName = "Fake Remote" });
        var coordinator = CreateCoordinator(remote);

        var reply = await coordinator.AskAsync(TestHelpers.Request("explain my budget"));

        Assert.Equal("remote answer", reply.Message);
        Assert.False(reply.IsFallback);
    }

    [Fact]
    public async Task AskAsync_WhenRemoteTimesOut_FallsBackAndExplainsWhy()
    {
        var remote = new FakeAiAssistant(new AiProviderTimeoutException("Fake Remote"));
        var coordinator = CreateCoordinator(remote);

        var reply = await coordinator.AskAsync(TestHelpers.Request("explain my budget"));

        Assert.True(reply.IsFallback);
        Assert.Equal("MoneyMap Built-in Assistant", reply.ProviderName);
        Assert.Contains("too long", reply.LimitationNotice);
    }

    [Fact]
    public async Task AskAsync_WhenRemoteReturnsMalformedResponse_FallsBack()
    {
        var remote = new FakeAiAssistant(new AiProviderMalformedResponseException("Fake Remote", "bad json"));
        var coordinator = CreateCoordinator(remote);

        var reply = await coordinator.AskAsync(TestHelpers.Request("explain my budget"));

        Assert.True(reply.IsFallback);
        Assert.Contains("unexpected response", reply.LimitationNotice);
    }

    [Fact]
    public async Task AskAsync_WhenRemoteUnavailable_FallsBackAndStillAnswers()
    {
        var remote = new FakeAiAssistant(new AiProviderUnavailableException("Fake Remote"));
        var coordinator = CreateCoordinator(remote);

        var reply = await coordinator.AskAsync(TestHelpers.Request("What is an emergency fund?"));

        Assert.True(reply.IsFallback);
        Assert.Contains("emergency fund is money", reply.Message);
    }

    [Fact]
    public async Task AskAsync_WhenRemoteThrowsUnexpectedException_DoesNotSwallowIt()
    {
        var remote = new FakeAiAssistant(new InvalidOperationException("boom"));
        var coordinator = CreateCoordinator(remote);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.AskAsync(TestHelpers.Request("hi")));
    }
}
