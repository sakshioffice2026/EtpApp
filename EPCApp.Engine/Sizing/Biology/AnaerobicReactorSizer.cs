using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Biology;

public static class AnaerobicReactorSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, double codInMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            codInMgL,
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_OLR", "OrganicLoadingKgCodPerM3PerDay"), 6.0),
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_COD_REMOVAL", "RemovalFraction"), 0.70),
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_BIOGAS_YIELD", "BiogasM3PerKgCodRemoved"), 0.45),
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_DEPTH", "WaterDepthM"), 6.0),
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_FREEBOARD", "FreeboardM"), 1.0),
            design.Rule(DesignBasis.RuleKey("AnaerobicReactor", "AR_UPFLOW", "MaxUpflowVelocityMPerHour"), 1.0));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double codInMgL,
        double organicLoadingKgCodPerM3PerDay = 6.0,
        double codRemovalFraction = 0.70,
        double biogasM3PerKgCodRemoved = 0.45,
        double waterDepthM = 6.0,
        double freeboardM = 1.0,
        double maxUpflowVelocityMPerHour = 1.0)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (codInMgL <= 0) throw new ArgumentOutOfRangeException(nameof(codInMgL));
        if (organicLoadingKgCodPerM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(organicLoadingKgCodPerM3PerDay));
        if (maxUpflowVelocityMPerHour <= 0) throw new ArgumentOutOfRangeException(nameof(maxUpflowVelocityMPerHour));

        var codLoadKgPerDay = flowM3PerDay * codInMgL / 1000.0;
        var volumeByLoadM3 = codLoadKgPerDay / organicLoadingKgCodPerM3PerDay;

        var flowM3PerHour = flowM3PerDay / 24.0;
        var minAreaM2 = flowM3PerHour / maxUpflowVelocityMPerHour;
        var volumeByUpflowM3 = minAreaM2 * waterDepthM;

        var volumeM3 = Math.Max(volumeByLoadM3, volumeByUpflowM3);
        var areaM2 = volumeM3 / waterDepthM;
        var diameterM = TankGeometry.CircularDiameter(areaM2);
        var totalDepth = waterDepthM + freeboardM;

        var hrtHours = volumeM3 / flowM3PerDay * 24.0;
        var upflowMPerHour = flowM3PerHour / areaM2;
        var codRemovedKgPerDay = codLoadKgPerDay * codRemovalFraction;
        var biogasM3PerDay = codRemovedKgPerDay * biogasM3PerKgCodRemoved;
        var codOutMgL = codInMgL * (1.0 - codRemovalFraction);

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["CodInMgL"] = codInMgL,
            ["CodOutMgL"] = Math.Round(codOutMgL, 1),
            ["CodLoadKgPerDay"] = Math.Round(codLoadKgPerDay, 2),
            ["OrganicLoadingKgCodPerM3PerDay"] = organicLoadingKgCodPerM3PerDay,
            ["VolumeM3"] = Math.Round(volumeM3, 2),
            ["HrtHours"] = Math.Round(hrtHours, 2),
            ["AreaM2"] = Math.Round(areaM2, 2),
            ["DiameterM"] = diameterM,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["UpflowVelocityMPerHour"] = Math.Round(upflowMPerHour, 2),
            ["CodRemovalFraction"] = codRemovalFraction,
            ["CodRemovedKgPerDay"] = Math.Round(codRemovedKgPerDay, 2),
            ["BiogasM3PerDay"] = Math.Round(biogasM3PerDay, 1)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.AnaerobicReactor,
            $"Anaerobic reactor {volumeM3:0.0} m3, {diameterM:0.0} m dia x {totalDepth:0.0} m depth",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}