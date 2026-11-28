using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Zld;

public static class MultiEffectEvaporatorSizer
{
    public static SizedEquipment Size(string tag, double feedM3PerDay, double feedTdsMgL, DesignBasis design) =>
        Size(
            tag,
            feedM3PerDay,
            feedTdsMgL,
            design.Rule(DesignBasis.RuleKey("Zld", "ZLD_MEE_CONCENTRATE", "ConcentrateFraction"), 0.20),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_EFFECTS", "Effects"), 3),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_ECONOMY_FACTOR", "EconomyPerEffect"), 0.85),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_OPERATING_HOURS", "HoursPerDay"), 22),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_U_VALUE", "UKwPerM2K"), 2.0),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_DELTA_T", "DeltaTPerEffectK"), 6),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_LATENT_HEAT", "KjPerKg"), 2257),
            design.Rule(DesignBasis.RuleKey("MultiEffectEvaporator", "MEE_CW_DELTA_T", "CoolingWaterDeltaTK"), 8));

    public static SizedEquipment Size(
        string tag,
        double feedM3PerDay,
        double feedTdsMgL,
        double concentrateFraction = 0.20,
        double effects = 3,
        double economyPerEffect = 0.85,
        double operatingHoursPerDay = 22,
        double uKwPerM2K = 2.0,
        double deltaTPerEffectK = 6,
        double latentHeatKjPerKg = 2257,
        double coolingWaterDeltaTK = 8)
    {
        if (feedM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(feedM3PerDay));
        if (effects < 1) throw new ArgumentOutOfRangeException(nameof(effects));
        if (economyPerEffect <= 0) throw new ArgumentOutOfRangeException(nameof(economyPerEffect));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (uKwPerM2K <= 0) throw new ArgumentOutOfRangeException(nameof(uKwPerM2K));
        if (deltaTPerEffectK <= 0) throw new ArgumentOutOfRangeException(nameof(deltaTPerEffectK));
        if (coolingWaterDeltaTK <= 0) throw new ArgumentOutOfRangeException(nameof(coolingWaterDeltaTK));

        concentrateFraction = Math.Clamp(concentrateFraction, 0.05, 1.0);

        var concentrateM3PerDay = feedM3PerDay * concentrateFraction;
        var evaporationM3PerDay = feedM3PerDay - concentrateM3PerDay;
        var concentrateTdsMgL = feedTdsMgL / concentrateFraction;

        var evaporationKgPerHour = evaporationM3PerDay * 1000.0 / operatingHoursPerDay;
        var steamEconomy = effects * economyPerEffect;
        var steamKgPerHour = evaporationKgPerHour / steamEconomy;

        var evaporationPerEffectKgPerSec = evaporationKgPerHour / effects / 3600.0;
        var areaPerEffectM2 = evaporationPerEffectKgPerSec * latentHeatKjPerKg / (uKwPerM2K * deltaTPerEffectK);
        var totalAreaM2 = areaPerEffectM2 * effects;

        var lastEffectVaporKgPerHour = evaporationKgPerHour / effects;
        var condenserDutyKw = lastEffectVaporKgPerHour / 3600.0 * latentHeatKjPerKg;
        var coolingWaterM3PerHour = condenserDutyKw / (4.18 * coolingWaterDeltaTK) * 3.6;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["FeedTdsMgL"] = Math.Round(feedTdsMgL, 1),
            ["ConcentrateFraction"] = concentrateFraction,
            ["ConcentrateM3PerDay"] = Math.Round(concentrateM3PerDay, 2),
            ["ConcentrateTdsMgL"] = Math.Round(concentrateTdsMgL, 0),
            ["EvaporationM3PerDay"] = Math.Round(evaporationM3PerDay, 2),
            ["OperatingHoursPerDay"] = operatingHoursPerDay,
            ["EvaporationKgPerHour"] = Math.Round(evaporationKgPerHour, 1),
            ["Effects"] = effects,
            ["EconomyPerEffect"] = economyPerEffect,
            ["SteamEconomy"] = Math.Round(steamEconomy, 2),
            ["SteamKgPerHour"] = Math.Round(steamKgPerHour, 1),
            ["LatentHeatKjPerKg"] = latentHeatKjPerKg,
            ["UKwPerM2K"] = uKwPerM2K,
            ["DeltaTPerEffectK"] = deltaTPerEffectK,
            ["AreaPerEffectM2"] = Math.Round(areaPerEffectM2, 1),
            ["HeatTransferAreaM2"] = Math.Round(totalAreaM2, 1),
            ["CondenserDutyKw"] = Math.Round(condenserDutyKw, 1),
            ["CoolingWaterM3PerHour"] = Math.Round(coolingWaterM3PerHour, 1)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.MultiEffectEvaporator,
            $"Multi-effect evaporator {effects:0} effects, {totalAreaM2:0} m2 heat transfer, {evaporationM3PerDay:0.0} m3/day evaporation",
            MaterialOfConstruction.Ss316,
            parameters);
    }
}
