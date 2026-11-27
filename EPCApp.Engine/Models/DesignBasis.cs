using EPCApp.Engine.Costing;

namespace EPCApp.Engine.Models;

public sealed class DesignBasis
{
    private readonly IReadOnlyDictionary<string, double> _rules;
    private readonly IReadOnlyDictionary<string, double> _rates;

    public DesignBasis()
        : this(new Dictionary<string, double>(), new Dictionary<string, double>())
    {
    }

    public DesignBasis(
        IReadOnlyDictionary<string, double> rules,
        IReadOnlyDictionary<string, double> rates)
    {
        _rules = new Dictionary<string, double>(rules, StringComparer.OrdinalIgnoreCase);
        _rates = new Dictionary<string, double>(rates, StringComparer.OrdinalIgnoreCase);
        Civil = RateSheet.FromItemCodes(_rates);
    }

    public RateSheet Civil { get; }

    public double Rule(string key, double fallback) =>
        _rules.TryGetValue(key, out var value) ? value : fallback;

    public double Rate(string itemCode, double fallback) =>
        _rates.TryGetValue(itemCode, out var value) ? value : fallback;

    public static string RuleKey(string category, string ruleCode, string parameter) =>
        $"{category}:{ruleCode}:{parameter}";
}
