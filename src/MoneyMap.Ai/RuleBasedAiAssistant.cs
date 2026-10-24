using System.Globalization;
using System.Text.RegularExpressions;
using MoneyMap.Application.Ai;
using MoneyMap.Domain;

namespace MoneyMap.Ai;

/// <summary>
/// The default, always-available assistant. Performs no network calls and requires no
/// credentials, so core AI functionality never depends on an external vendor being reachable.
/// Every number it states comes directly from the supplied <see cref="AiFinancialContext"/> or
/// from a Domain-layer calculator — it never invents a figure.
/// </summary>
public sealed partial class RuleBasedAiAssistant : IAiAssistant
{
    public string ProviderName => "MoneyMap Built-in Assistant";

    public bool RequiresNetwork => false;

    public Task<AiAssistantReply> AskAsync(AiAssistantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var message = request.UserMessage.Trim();
        var lower = message.ToLowerInvariant();
        var ctx = request.Context;

        string reply = lower switch
        {
            _ when ContainsAny(lower, "afford") => AnswerAffordability(message, ctx),
            _ when ContainsAny(lower, "compare") && CountPercentages(message) >= 2 => CompareHypotheticalReturns(message, ctx),
            _ when ContainsAny(lower, "over budget", "exceed", "overspending", "over their planned") => AnswerOverBudget(ctx),
            _ when ContainsAny(lower, "saving", "savings", "goal") => AnswerSavingsGoals(ctx),
            _ when ContainsAny(lower, "credit") => Explanations.Credit,
            _ when ContainsAny(lower, "debt") => Explanations.Debt,
            _ when ContainsAny(lower, "inflation") => Explanations.Inflation,
            _ when ContainsAny(lower, "compound") => Explanations.CompoundInterest,
            _ when ContainsAny(lower, "emergency fund") => Explanations.EmergencyFund,
            _ when ContainsAny(lower, "budget", "explain", "summary", "summarize") => AnswerBudgetSummary(ctx),
            _ => Explanations.Fallback,
        };

        return Task.FromResult(new AiAssistantReply
        {
            Message = reply,
            ProviderName = ProviderName,
            IsFallback = false,
        });
    }

    private static bool ContainsAny(string haystack, params string[] needles) => needles.Any(haystack.Contains);

    private static string AnswerBudgetSummary(AiFinancialContext ctx)
    {
        if (ctx.MonthlyNetIncome <= 0m)
        {
            return "I don't see an active monthly income entered yet, so I can't calculate remaining funds or budget utilization. Add an income source on the Onboarding page and ask me again.";
        }

        var utilization = ctx.BudgetUtilizationPercent is { } pct
            ? $" That's {pct.ToString("0.#", CultureInfo.InvariantCulture)}% of your income planned to be spent."
            : string.Empty;

        var remainingWord = ctx.PlannedRemainingFunds < 0 ? "a shortfall of" : "remaining funds of";
        var remainingAmount = Math.Abs(ctx.PlannedRemainingFunds);

        return $"Based on your entries: monthly take-home income is {FormatMoney(ctx.MonthlyNetIncome, ctx.Currency)}, " +
               $"planned expenses total {FormatMoney(ctx.PlannedExpenses, ctx.Currency)}, leaving {remainingWord} {FormatMoney(remainingAmount, ctx.Currency)}.{utilization}";
    }

    private static string AnswerOverBudget(AiFinancialContext ctx)
    {
        if (ctx.OverBudgetCategoryNames.Count == 0)
        {
            return "Nothing is currently recorded as spending more than planned. Record actual amounts on the Budget Builder page to track this over time.";
        }

        return "These items are currently over their planned amount: " + string.Join(", ", ctx.OverBudgetCategoryNames) + ".";
    }

    private static string AnswerSavingsGoals(AiFinancialContext ctx)
    {
        if (ctx.SavingsGoals.Count == 0)
        {
            return "You don't have any savings goals set up yet. Add one on the Savings Goals page with a target amount and, optionally, a deadline.";
        }

        var lines = ctx.SavingsGoals.Select(g =>
        {
            var remaining = Math.Max(0m, g.TargetAmount - g.CurrentAmount);
            if (remaining <= 0m)
            {
                return $"'{g.Name}' has already reached its {FormatMoney(g.TargetAmount, ctx.Currency)} target.";
            }

            if (g.Deadline.HasValue && g.RequiredMonthlyContribution.HasValue)
            {
                return $"'{g.Name}' needs about {FormatMoney(g.RequiredMonthlyContribution.Value, ctx.Currency)}/month to reach {FormatMoney(g.TargetAmount, ctx.Currency)} by {g.Deadline:MMMM yyyy}.";
            }

            if (g.PlannedMonthlyContribution is { } contribution and > 0m)
            {
                var months = Math.Ceiling(remaining / contribution);
                return $"'{g.Name}' has {FormatMoney(remaining, ctx.Currency)} left; at {FormatMoney(contribution, ctx.Currency)}/month that's about {months} more month(s).";
            }

            return $"'{g.Name}' has {FormatMoney(remaining, ctx.Currency)} left to reach its goal, but no monthly contribution is set yet.";
        });

        return string.Join(" ", lines);
    }

    private static string AnswerAffordability(string message, AiFinancialContext ctx)
    {
        var amount = ExtractFirstDollarAmount(message);
        if (amount is null)
        {
            return "Tell me a specific dollar amount (for example, \"Can I afford $75 for new shoes?\") and I'll compare it against your remaining planned funds.";
        }

        if (ctx.MonthlyNetIncome <= 0m)
        {
            return "I can't evaluate affordability yet because no income is entered. Add your income first.";
        }

        var remaining = ctx.PlannedRemainingFunds;
        if (amount.Value <= remaining)
        {
            var after = remaining - amount.Value;
            return $"Based on your current plan, {FormatMoney(amount.Value, ctx.Currency)} fits: you have {FormatMoney(remaining, ctx.Currency)} in planned remaining funds, leaving {FormatMoney(after, ctx.Currency)} after this purchase.";
        }

        var shortfall = amount.Value - remaining;
        return $"That would likely strain your budget: you have {FormatMoney(remaining, ctx.Currency)} in planned remaining funds, which is {FormatMoney(shortfall, ctx.Currency)} short of {FormatMoney(amount.Value, ctx.Currency)}.";
    }

    private static string CompareHypotheticalReturns(string message, AiFinancialContext ctx)
    {
        var rates = ExtractPercentages(message).Take(2).ToList();
        var years = ExtractFirstInteger(message, "year") ?? 10;
        var monthly = 100m;

        var results = rates.Select(rate =>
        {
            var projection = InvestmentSimulator.Simulate(new InvestmentAssumptions
            {
                InitialInvestment = new Money(0m, ctx.Currency),
                MonthlyContribution = new Money(monthly, ctx.Currency),
                YearsHorizon = years,
                AnnualReturnPercent = rate,
            });
            return $"{rate.ToString("0.#", CultureInfo.InvariantCulture)}% a year could hypothetically grow to about {FormatMoney(projection.ProjectedEndingBalance.Amount, ctx.Currency)}";
        });

        return $"Purely hypothetically, contributing {FormatMoney(monthly, ctx.Currency)}/month for {years} years: " + string.Join("; ", results) +
               ". This is a simplified, hypothetical projection, not a prediction — real returns vary and are never guaranteed. Visit the Investment Simulator for full assumptions.";
    }

    private static decimal? ExtractFirstDollarAmount(string message)
    {
        var match = DollarAmountRegex().Match(message);
        return match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static List<decimal> ExtractPercentages(string message)
        => PercentRegex().Matches(message)
            .Select(m => decimal.TryParse(m.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : (decimal?)null)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

    private static int CountPercentages(string message) => PercentRegex().Matches(message).Count;

    private static int? ExtractFirstInteger(string message, string nearWord)
    {
        var match = Regex.Match(message, @"(\d+)\s*" + Regex.Escape(nearWord), RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var v) ? v : null;
    }

    private static string FormatMoney(decimal amount, string currency) => new Money(amount, currency).ToString();

    [GeneratedRegex(@"\$\s?([0-9]+(?:\.[0-9]{1,2})?)")]
    private static partial Regex DollarAmountRegex();

    [GeneratedRegex(@"([0-9]+(?:\.[0-9]+)?)\s?%")]
    private static partial Regex PercentRegex();

    private static class Explanations
    {
        public const string Credit =
            "Credit is money a lender gives you now that you agree to pay back later, usually with interest. Your credit history (how reliably you've repaid in the past) " +
            "affects the interest rate you're offered and whether you're approved. Using less than you're able to borrow and paying on time are two of the biggest factors in building it.";

        public const string Debt =
            "Debt is money owed to someone else. Not all debt is the same: a loan for school or a home (often lower interest) behaves very differently from high-interest credit card debt. " +
            "Two common payoff strategies are the 'avalanche' (pay off the highest interest rate first, usually cheapest overall) and the 'snowball' (pay off the smallest balance first, for motivation). Neither is automatically right for everyone.";

        public const string Inflation =
            "Inflation is the general rise in prices over time, which reduces how much a fixed amount of money can buy in the future. If prices rise 3% a year, something that costs $100 today " +
            "would typically cost about $103 next year. That's why long-term savings and investment plans often account for inflation separately from nominal returns.";

        public const string CompoundInterest =
            "Compound interest is interest earned on both your original amount and on interest you've already earned, so growth accelerates over time compared to simple interest. " +
            "The earlier money is invested or saved, the more time compounding has to work — this is why starting small but starting early is often emphasized over waiting to save a large amount.";

        public const string EmergencyFund =
            "An emergency fund is money set aside specifically for unplanned expenses (a car repair, a medical bill, a period without income) so you don't have to rely on high-interest debt. " +
            "A common starting target is three to six months of essential expenses, but any amount is a meaningful start — you can track progress toward one on the Savings Goals page.";

        public const string Fallback =
            "I can explain your budget, point out categories that are over their planned amount, help you think through a savings goal, answer a specific affordability question (try \"Can I afford $50?\"), " +
            "or explain concepts like credit, debt, inflation, compound interest, and emergency funds. What would you like to know?";
    }
}
