using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Clarifiers;

public static class PrimaryClarifierSizer
{
    private const double MaxDiameterM = 30.0;

    public static SizedEquipment Size(string tag, double flowM3PerDay, double tssMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            tssMgL,
            design.Rule(DesignBasis.RuleKey("PrimaryClarifier", "PC_SOR", "SurfaceOverflowRate"), 30),
            design.Rule(DesignBasis.RuleKey("PrimaryClarifier", "PC_TSS_REMOVAL", "RemovalFraction"), 0.60),
            design.Rule(DesignBasis.RuleKey("PrimaryClarifier", "PC_SLUDGE_SOLIDS", "SludgeSolidsFraction"), 0.04),
            design.Rule(DesignBasis.RuleKey("PrimaryClarifier", "PC_DEPTH", "WaterDepthM"), 3.0),
            design.Rule(DesignBasis.RuleKey("PrimaryClarifier", "PC_FREEBOARD", "FreeboardM"), 0.5));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double tssMgL,
        double surfaceOverflowRate = 30,
        double tssRemovalFraction = 0.60,
        double sludgeSolidsFraction = 0.04,
        double waterDepthM = 3.0,
        double freeboardM = 0.5)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (surfaceOverflowRate <= 0) throw new ArgumentOutOfRangeException(nameof(surfaceOverflowRate));
        if (sludgeSolidsFraction <= 0) throw new ArgumentOutOfRangeException(nameof(sludgeSolidsFraction));

        var totalAreaM2 = flowM3PerDay / surfaceOverflowRate;
        var maxUnitAreaM2 = Math.PI / 4.0 * MaxDiameterM * MaxDiameterM;
        var units = Math.Max(1, (int)Math.Ceiling(totalAreaM2 / maxUnitAreaM2));
        var unitAreaM2 = totalAreaM2 / units;
        var diameterM = TankGeometry.CircularDiameter(unitAreaM2);
        var totalDepth = waterDepthM + freeboardM;

        var hrtHours = totalAreaM2 * waterDepthM / flowM3PerDay * 24.0;
        var solidsRemovedKgPerDay = flowM3PerDay * tssMgL / 1000.0 * tssRemovalFraction;
        var sludgeM3PerDay = solidsRemovedKgPerDay / (sludgeSolidsFraction * 1000.0);

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["TssInMgL"] = tssMgL,
            ["SurfaceOverflowRateM3PerM2PerDay"] = surfaceOverflowRate,
            ["TotalAreaM2"] = Math.Round(totalAreaM2, 2),
            ["Units"] = units,
            ["DiameterM"] = diameterM,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["HrtHours"] = Math.Round(hrtHours, 2),
            ["TssRemovalFraction"] = tssRemovalFraction,
            ["SolidsRemovedKgPerDay"] = Math.Round(solidsRemovedKgPerDay, 2),
            ["SludgeSolidsFraction"] = sludgeSolidsFraction,
            ["SludgeM3PerDay"] = Math.Round(sludgeM3PerDay, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.PrimaryClarifier,
            $"Primary clarifier {units} no. x {diameterM:0.0} m dia x {totalDepth:0.0} m depth",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}