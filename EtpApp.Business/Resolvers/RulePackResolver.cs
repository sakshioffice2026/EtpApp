using EtpApp.Database.Enums;
using EtpApp.Engine.Abstractions;

namespace EtpApp.Business.Resolvers;

public sealed class RulePackResolver
{
    private readonly IReadOnlyDictionary<PlantType, IPlantRulePack> _packs;

    public RulePackResolver(IEnumerable<IPlantRulePack> packs)
    {
        _packs = packs.ToDictionary(x => x.PlantType);
    }

    public IPlantRulePack Resolve(PlantType plantType)
    {
        if (_packs.TryGetValue(plantType, out var pack))
            return pack;

        throw new InvalidOperationException($"No rule pack registered for plant type '{plantType}'.");
    }
}