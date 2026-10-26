namespace MoneyMap.Web.Components.Shared;

// One labeled value for the chart components - plain decimal so charts don't depend on a Domain type.
public sealed record ChartDatum(string Label, decimal Value, string Currency, decimal? SecondaryValue = null, string? SecondaryLabel = null);
