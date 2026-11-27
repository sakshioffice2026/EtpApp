using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Zld;

public static class AtfdCrystallizerSizer
{
    public static SizedEquipment Size(string tag, double feedM3PerDay, double tdsMgL, DesignBasis design) =>
        Size(
            tag,
            feedM3PerDay,
            tdsMgL,
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_EVAP_RATE", "KgPerM2PerHour"), 20),
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_STEAM_ECON", "KgSteamPerKgWater"), 1.25),
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_OPERATING_HOURS", "HoursPerDay"), 22),
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_RESIDUAL_MOISTURE", "MoistureFraction"), 0.05),
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_MAX_UNIT_AREA", "MaxAreaM2PerUnit"), 12),
            design.Rule(DesignBasis.RuleKey("AtfdCrystallizer", "ATFD_CRYSTALLIZER_FEED", "FeedTdsFraction"), 0.25));

    public static SizedEquipment Size(
        string tag,
        double feedM3PerDay,
        double tdsMgL,
        double evaporationKgPerM2PerHour = 20,
        double steamKgPerKgWater = 1.25,
        double operatingHoursPerDay = 22,
        double residualMoistureFraction = 0.05,
        double maxUnitAreaM2 = 12,
        double crystallizerFeedTdsFraction = 0.25)
    {
        if (feedM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(feedM3PerDay));
        if (evaporationKgPerM2PerHour <= 0) throw new ArgumentOutOfRangeException(nameof(evaporationKgPerM2PerHour));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (maxUnitAreaM2 <= 0) throw new ArgumentOutOfRangeException(nameof(maxUnitAreaM2));

        var feedKgPerDay = feedM3PerDay * 1000.0;
        var solidsKgPerDay = feedM3PerDay * tdsMgL / 1000.0;
        var dryProductKgPerDay = solidsKgPerDay / (1.0 - residualMoistureFraction);
        var waterEvaporatedKgPerDay = Math.Max(feedKgPerDay - dryProductKgPerDay, 0);
        var waterEvaporatedKgPerHour = waterEvaporatedKgPerDay / operatingHoursPerDay;

        var totalAreaM2 = waterEvaporatedKgPerHour / evaporationKgPerM2PerHour;
        var units = Math.Max(1, (int)Math.Ceiling(totalAreaM2 / maxUnitAreaM2));
        var unitAreaM2 = totalAreaM2 / units;

        var steamKgPerHour = waterEvaporatedKgPerHour * steamKgPerKgWater;

        var crystallizerFeedKgPerDay = crystallizerFeedTdsFraction > 0
            ? solidsKgPerDay / crystallizerFeedTdsFraction
            : feedKgPerDay;

        var parameters = new Dictionary<string, double>
        {
            ["FeedM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["TdsMgL"] = tdsMgL,
            ["SolidsKgPerDay"] = Math.Round(solidsKgPerDay, 1),
            ["DryProductKgPerDay"] = Math.Round(dryProductKgPerDay, 1),
            ["WaterEvaporatedKgPerHour"] = Math.Round(waterEvaporatedKgPerHour, 1),
            ["OperatingHoursPerDay"] = operatingHoursPerDay,
            ["EvaporationKgPerM2PerHour"] = evaporationKgPerM2PerHour,
            ["HeatTransferAreaM2"] = Math.Round(totalAreaM2, 2),
            ["Units"] = units,
            ["UnitAreaM2"] = Math.Round(unitAreaM2, 2),
            ["SteamKgPerHour"] = Math.Round(steamKgPerHour, 1),
            ["CrystallizerFeedKgPerDay"] = Math.Round(crystallizerFeedKgPerDay, 1),
            ["CondensateM3PerDay"] = Math.Round(waterEvaporatedKgPerDay / 1000.0, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.AtfdCrystallizer,
            $"ATFD with crystallizer {units} no. x {unitAreaM2:0.0} m2, {waterEvaporatedKgPerHour:0} kg/h evaporation (staged sizing)",
            MaterialOfConstruction.Ss316,
            parameters);
    }
}