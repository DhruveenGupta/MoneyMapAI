using MoneyMap.Application.Ai;

namespace MoneyMap.Ai.Tests;

internal static class TestHelpers
{
    public static AiFinancialContext SampleContext(decimal income = 3000m, decimal planned = 1000m, decimal? actual = null) => new()
    {
        Currency = "USD",
        MonthlyNetIncome = income,
        PlannedExpenses = planned,
        ActualExpenses = actual,
        PlannedRemainingFunds = income - planned,
        BudgetUtilizationPercent = income > 0 ? Math.Round(planned / income * 100m, 1) : null,
    };

    public static AiAssistantRequest Request(string message, AiFinancialContext? context = null) => new()
    {
        UserMessage = message,
        Context = context ?? SampleContext(),
    };
}

internal sealed class FakeAiAssistant : IAiAssistant
{
    private readonly Func<AiAssistantRequest, AiAssistantReply>? _respond;
    private readonly Exception? _throw;

    public FakeAiAssistant(Func<AiAssistantRequest, AiAssistantReply> respond) => _respond = respond;

    public FakeAiAssistant(Exception toThrow) => _throw = toThrow;

    public string ProviderName => "Fake Remote";

    public bool RequiresNetwork => true;

    public Task<AiAssistantReply> AskAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (_throw is not null)
        {
            throw _throw;
        }

        return Task.FromResult(_respond!(request));
    }
}
