using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Engine.Models;
using EPCApp.Repositories.Contracts;

namespace EPCApp.Business.Resolvers;

public interface IDesignBasisLoader
{
    Task<DesignBasis> LoadAsync(PlantType plantType, CancellationToken cancellationToken = default);
}

public sealed class DesignBasisLoader : IDesignBasisLoader
{
    private readonly IDesignRuleRepository _designRules;
    private readonly IRateCardRepository _rateCards;

    public DesignBasisLoader(IDesignRuleRepository designRules, IRateCardRepository rateCards)
    {
        _designRules = designRules;
        _rateCards = rateCards;
    }

    public async Task<DesignBasis> LoadAsync(PlantType plantType, CancellationToken cancellationToken = default)
    {
        var rules = await _designRules.GetActiveByPlantAsync(plantType, cancellationToken);
        var cards = await _rateCards.GetActiveAsync(null, cancellationToken);

        var ruleValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules.Where(x => x.PlantType == PlantType.All))
        {
            Apply(ruleValues, rule);
        }

        foreach (var rule in rules.Where(x => x.PlantType != PlantType.All))
        {
            Apply(ruleValues, rule);
        }

        var rateValues = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var card in cards)
        {
            rateValues[card.ItemCode] = (double)card.Rate;
        }

        return new DesignBasis(ruleValues, rateValues);
    }

    private static void Apply(Dictionary<string, double> target, DesignRule rule)
    {
        double value;

        if (rule.MinValue.HasValue && rule.MaxValue.HasValue)
        {
            value = ((double)rule.MinValue.Value + (double)rule.MaxValue.Value) / 2.0;
        }
        else if (rule.MaxValue.HasValue)
        {
            value = (double)rule.MaxValue.Value;
        }
        else if (rule.MinValue.HasValue)
        {
            value = (double)rule.MinValue.Value;
        }
        else
        {
            return;
        }

        target[DesignBasis.RuleKey(rule.Category, rule.RuleCode, rule.Parameter)] = value;
    }
}
