using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MoneyMap.Application.Ai;
using MoneyMap.Domain;

namespace MoneyMap.Ai;

// Pulls the ```proposed-changes block out of an LLM response and validates each entry - bad
// category, huge amount, missing rationale, whatever doesn't pass just gets dropped quietly.
internal static partial class ProposedChangeParser
{
    private const decimal MaxReasonableMonthlyDelta = 1_000_000m;

    public static (string CleanText, IReadOnlyList<ProposedBudgetChange> Changes) ExtractAndStrip(string text, ILogger logger)
    {
        var match = ProposedChangesBlockRegex().Match(text);
        if (!match.Success)
        {
            return (text.Trim(), Array.Empty<ProposedBudgetChange>());
        }

        var cleanText = ProposedChangesBlockRegex().Replace(text, string.Empty).Trim();
        var json = match.Groups[1].Value;
        var changes = new List<ProposedBudgetChange>();

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return (cleanText, changes);
            }

            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (TryValidate(element, out var change))
                {
                    changes.Add(change!);
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Discarding malformed proposed-changes block from AI response.");
        }

        return (cleanText, changes);
    }

    private static bool TryValidate(JsonElement element, out ProposedBudgetChange? change)
    {
        change = null;

        if (!element.TryGetProperty("category", out var categoryProp) || categoryProp.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        if (!Enum.TryParse<ExpenseCategory>(categoryProp.GetString(), ignoreCase: true, out var category))
        {
            return false;
        }

        if (!element.TryGetProperty("deltaAmount", out var deltaProp) || !deltaProp.TryGetDecimal(out var delta))
        {
            return false;
        }

        if (delta == 0m || Math.Abs(delta) > MaxReasonableMonthlyDelta)
        {
            return false;
        }

        var rationale = element.TryGetProperty("rationale", out var rationaleProp) && rationaleProp.ValueKind == JsonValueKind.String
            ? rationaleProp.GetString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(rationale))
        {
            return false;
        }

        change = new ProposedBudgetChange { Category = category, DeltaAmount = delta, Rationale = rationale.Trim() };
        return true;
    }

    [GeneratedRegex(@"```proposed-changes\s*(\[[\s\S]*?\])\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex ProposedChangesBlockRegex();
}
