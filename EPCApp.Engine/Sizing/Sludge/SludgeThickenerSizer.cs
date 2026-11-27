using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Sludge;

public static class SludgeThickenerSizer
{
    private const double MaxDiameterM = 20.0;

    public static SizedEquipment Size(string tag, TrainPlan plan, DesignBasis design) =>
        Size(
            tag,
            plan,
            design.Rule(DesignBasis.RuleKey("SludgeThickener", "ST_SOLIDS_LOADING", "KgPerM2PerDay"), 40),
            design.Rule(DesignBasis.RuleKey("SludgeThickener", "ST_UNDERFLOW_SOLIDS", "SolidsFraction"), 0.03),
            design.Rule(DesignBasis.RuleKey("SludgeThickener", "ST_CAPTURE", "SolidsCaptureFraction"), 0.90),
            design.Rule(DesignBasis.RuleKey("SludgeThickener", "ST_DEPTH", "WaterDepthM"), 3.0),
            design.Rule(DesignBasis.RuleKey("SludgeThickener", "ST_FREEBOARD", "FreeboardM"), 0.4));

    public static SizedEquipment Size(
        string tag,
        TrainPlan plan,
        double solidsLoadingKgPerM2PerDay = 40,
        double underflowSolidsFraction = 0.03,
        double solidsCaptureFraction = 0.90,
        double waterDepthM = 3.0,
        double freeboardM = 0.4)
    {
        if (plan.SludgeDryKgPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(plan), "Sludge dry solids must be computed first.");
        if (solidsLoadingKgPerM2PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(solidsLoadingKgPerM2PerDay));
        if (underflowSolidsFraction <= 0) throw new ArgumentOutOfRangeException(nameof(underflowSolidsFraction));

        var feedDryKgPerDay = plan.SludgeDryKgPerDay;
        var feedM3PerDay = plan.SludgeM3PerDay;

        var totalAreaM2 = feedDryKgPerDay / solidsLoadingKgPerM2PerDay;
        var maxUnitAreaM2 = Math.PI / 4.0 * MaxDiameterM * MaxDiameterM;
        var units = Math.Max(1, (int)Math.Ceiling(totalAreaM2 / maxUnitAreaM2));
        var diameterM = TankGeometry.CircularDiameter(totalAreaM2 / units);
        var totalDepth = waterDepthM + freeboardM;

        var capturedKgPerDay = feedDryKgPerDay * solidsCaptureFraction;
        var underflowM3PerDay = capturedKgPerDay / (underflowSolidsFraction * 1000.0);
        var supernatantM3PerDay = Math.Max(feedM3PerDay - underflowM3PerDay, 0);

        var parameters = new Dictionary<string, double>
        {
            ["FeedM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["FeedDryKgPerDay"] = Math.Round(feedDryKgPerDay, 2),
            ["SolidsLoadingKgPerM2PerDay"] = solidsLoadingKgPerM2PerDay,
            ["TotalAreaM2"] = Math.Round(totalAreaM2, 2),
            ["Units"] = units,
            ["DiameterM"] = diameterM,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["SolidsCaptureFraction"] = solidsCaptureFraction,
            ["UnderflowSolidsFraction"] = underflowSolidsFraction,
            ["UnderflowM3PerDay"] = Math.Round(underflowM3PerDay, 2),
            ["SupernatantM3PerDay"] = Math.Round(supernatantM3PerDay, 2)
        };

        plan.SludgeDryKgPerDay = Math.Round(capturedKgPerDay, 2);
        plan.SludgeM3PerDay = Math.Round(underflowM3PerDay, 2);
        plan.TreatedWaterM3PerDay = Math.Round(Math.Max(plan.InletFlowM3PerDay - underflowM3PerDay, 0), 2);
        plan.AddNote($"Sludge thickener: underflow {plan.SludgeM3PerDay:0.0} m3/day at {underflowSolidsFraction * 100:0.0}% solids; supernatant {supernatantM3PerDay:0.0} m3/day returns to equalization.");

        return new SizedEquipment(
            tag,
            UnitOperationType.SludgeThickener,
            $"Sludge thickener {units} no. x {diameterM:0.0} m dia x {totalDepth:0.0} m depth",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}