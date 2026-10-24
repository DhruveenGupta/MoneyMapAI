using Microsoft.Extensions.Logging.Abstractions;
using MoneyMap.Domain;
using Xunit;

namespace MoneyMap.Ai.Tests;

public class ProposedChangeParserTests
{
    private static readonly NullLogger Logger = NullLogger.Instance;

    [Fact]
    public void ExtractAndStrip_WithNoBlock_ReturnsTextUnchangedAndNoChanges()
    {
        var (text, changes) = ProposedChangeParser.ExtractAndStrip("Just a plain answer.", Logger);

        Assert.Equal("Just a plain answer.", text);
        Assert.Empty(changes);
    }

    [Fact]
    public void ExtractAndStrip_WithValidBlock_ParsesAndStripsIt()
    {
        var input = "Here's my suggestion.\n```proposed-changes\n[{\"category\":\"Discretionary\",\"deltaAmount\":-50,\"rationale\":\"Eat out less\"}]\n```";

        var (text, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Equal("Here's my suggestion.", text);
        var change = Assert.Single(changes);
        Assert.Equal(ExpenseCategory.Discretionary, change.Category);
        Assert.Equal(-50m, change.DeltaAmount);
        Assert.Equal("Eat out less", change.Rationale);
    }

    [Fact]
    public void ExtractAndStrip_WithUnknownCategory_DropsEntry()
    {
        var input = "```proposed-changes\n[{\"category\":\"NotARealCategory\",\"deltaAmount\":-10,\"rationale\":\"x\"}]\n```";

        var (_, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Empty(changes);
    }

    [Fact]
    public void ExtractAndStrip_WithZeroDelta_DropsEntry()
    {
        var input = "```proposed-changes\n[{\"category\":\"Housing\",\"deltaAmount\":0,\"rationale\":\"x\"}]\n```";

        var (_, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Empty(changes);
    }

    [Fact]
    public void ExtractAndStrip_WithUnreasonablyLargeDelta_DropsEntry()
    {
        var input = "```proposed-changes\n[{\"category\":\"Housing\",\"deltaAmount\":99999999,\"rationale\":\"x\"}]\n```";

        var (_, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Empty(changes);
    }

    [Fact]
    public void ExtractAndStrip_WithMissingRationale_DropsEntry()
    {
        var input = "```proposed-changes\n[{\"category\":\"Housing\",\"deltaAmount\":-10}]\n```";

        var (_, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Empty(changes);
    }

    [Fact]
    public void ExtractAndStrip_WithMalformedJson_DoesNotThrow_ReturnsNoChanges()
    {
        var input = "```proposed-changes\n[{not: valid, json}]\n```";

        var exception = Record.Exception(() => ProposedChangeParser.ExtractAndStrip(input, Logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ExtractAndStrip_WithMultipleValidEntries_ReturnsAll()
    {
        var input = "```proposed-changes\n[{\"category\":\"Housing\",\"deltaAmount\":-10,\"rationale\":\"a\"},{\"category\":\"Groceries\",\"deltaAmount\":20,\"rationale\":\"b\"}]\n```";

        var (_, changes) = ProposedChangeParser.ExtractAndStrip(input, Logger);

        Assert.Equal(2, changes.Count);
    }
}
