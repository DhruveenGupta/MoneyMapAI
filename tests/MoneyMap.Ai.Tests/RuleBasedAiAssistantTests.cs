using MoneyMap.Application.Ai;
using Xunit;

namespace MoneyMap.Ai.Tests;

public class RuleBasedAiAssistantTests
{
    private readonly RuleBasedAiAssistant _assistant = new();

    [Fact]
    public void RequiresNetwork_IsFalse()
    {
        Assert.False(_assistant.RequiresNetwork);
    }

    [Fact]
    public async Task AskAsync_ExplainBudget_UsesContextNumbers()
    {
        var context = TestHelpers.SampleContext(income: 3000m, planned: 1000m);

        var reply = await _assistant.AskAsync(TestHelpers.Request("Can you explain my budget?", context));

        Assert.Contains("3,000.00", reply.Message);
        Assert.Contains("1,000.00", reply.Message);
        Assert.False(reply.IsFallback);
    }

    [Fact]
    public async Task AskAsync_WithZeroIncome_SaysSoInsteadOfDividingByZero()
    {
        var context = TestHelpers.SampleContext(income: 0m, planned: 500m);

        var reply = await _assistant.AskAsync(TestHelpers.Request("explain my budget", context));

        Assert.Contains("don't see an active monthly income", reply.Message);
    }

    [Fact]
    public async Task AskAsync_OverBudgetQuestion_ListsOverBudgetCategories()
    {
        var context = TestHelpers.SampleContext() with { OverBudgetCategoryNames = new[] { "Groceries" } };

        var reply = await _assistant.AskAsync(TestHelpers.Request("Am I over budget anywhere?", context));

        Assert.Contains("Groceries", reply.Message);
    }

    [Fact]
    public async Task AskAsync_AffordabilityQuestion_WithinBudget_SaysItFits()
    {
        var context = TestHelpers.SampleContext(income: 3000m, planned: 1000m); // remaining = 2000

        var reply = await _assistant.AskAsync(TestHelpers.Request("Can I afford $50 for shoes?", context));

        Assert.Contains("fits", reply.Message);
    }

    [Fact]
    public async Task AskAsync_AffordabilityQuestion_ExceedsBudget_SaysItStrains()
    {
        var context = TestHelpers.SampleContext(income: 1000m, planned: 950m); // remaining = 50

        var reply = await _assistant.AskAsync(TestHelpers.Request("Can I afford $500 for a trip?", context));

        Assert.Contains("strain", reply.Message);
    }

    [Fact]
    public async Task AskAsync_AffordabilityQuestion_WithoutAmount_AsksForOne()
    {
        var reply = await _assistant.AskAsync(TestHelpers.Request("Can I afford it?"));

        Assert.Contains("specific dollar amount", reply.Message);
    }

    [Fact]
    public async Task AskAsync_CompareReturns_IncludesHypotheticalDisclaimer()
    {
        var reply = await _assistant.AskAsync(TestHelpers.Request("Compare 5% and 8% returns over 10 years"));

        Assert.Contains("hypothetical", reply.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5%", reply.Message);
        Assert.Contains("8%", reply.Message);
    }

    [Theory]
    [InlineData("What is credit?", "Credit is money")]
    [InlineData("Explain debt to me", "Debt is money")]
    [InlineData("What is inflation?", "Inflation is the general rise")]
    [InlineData("How does compound interest work?", "Compound interest is interest")]
    [InlineData("What's an emergency fund?", "emergency fund is money")]
    public async Task AskAsync_EducationalConcepts_ReturnsExpectedExplanation(string question, string expectedSubstring)
    {
        var reply = await _assistant.AskAsync(TestHelpers.Request(question));

        Assert.Contains(expectedSubstring, reply.Message);
    }

    [Fact]
    public async Task AskAsync_UnrecognizedQuestion_ReturnsHelpfulFallback()
    {
        var reply = await _assistant.AskAsync(TestHelpers.Request("asdkjfh qwoeiru"));

        Assert.Contains("explain your budget", reply.Message);
    }

    [Fact]
    public async Task AskAsync_SavingsGoalWithDeadline_ReportsRequiredContribution()
    {
        var context = TestHelpers.SampleContext() with
        {
            SavingsGoals = new[]
            {
                new AiGoalSnapshot { Name = "Trip", TargetAmount = 1200m, CurrentAmount = 0m, Deadline = new DateOnly(2026, 7, 1), RequiredMonthlyContribution = 200m },
            },
        };

        var reply = await _assistant.AskAsync(TestHelpers.Request("How are my savings goals going?", context));

        Assert.Contains("Trip", reply.Message);
        Assert.Contains("200.00", reply.Message);
    }
}
