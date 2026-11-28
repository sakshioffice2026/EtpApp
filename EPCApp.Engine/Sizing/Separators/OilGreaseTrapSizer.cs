using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Separators;

public static class OilGreaseTrapSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, double oilGreaseMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            oilGreaseMgL,
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_PEAK_FACTOR", "PeakFactor"), 2.0),
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_RETENTION", "RetentionMinutes"), 30),
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_DEPTH", "WaterDepthM"), 1.5),
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_FREEBOARD", "FreeboardM"), 0.3),
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_LENGTH_WIDTH", "LengthToWidth"), 3.0),
            design.Rule(DesignBasis.RuleKey("OilGreaseTrap", "OGT_REMOVAL", "RemovalFraction"), 0.60));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double oilGreaseMgL,
        double peakFactor = 2.0,
        double retentionMinutes = 30,
        double waterDepthM = 1.5,
        double freeboardM = 0.3,
        double lengthToWidth = 3.0,
        double removalFraction = 0.60)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (peakFactor < 1) throw new ArgumentOutOfRangeException(nameof(peakFactor));
        if (retentionMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(retentionMinutes));
        if (waterDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(waterDepthM));

        removalFraction = Math.Clamp(removalFraction, 0.0, 0.95);

        var peakFlowM3PerHour = flowM3PerDay / 24.0 * peakFactor;
        var volumeM3 = peakFlowM3PerHour * retentionMinutes / 60.0;
        var (length, width) = TankGeometry.Rectangular(volumeM3, waterDepthM, lengthToWidth);
        var totalDepth = waterDepthM + freeboardM;

        var oilLoadKgPerDay = flowM3PerDay * oilGreaseMgL / 1000.0;
        var oilRemovedKgPerDay = oilLoadKgPerDay * removalFraction;
        var oilOutMgL = oilGreaseMgL * (1.0 - removalFraction);

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["OilGreaseInMgL"] = Math.Round(oilGreaseMgL, 2),
            ["PeakFactor"] = peakFactor,
            ["PeakFlowM3PerHour"] = Math.Round(peakFlowM3PerHour, 2),
            ["RetentionMinutes"] = retentionMinutes,
            ["VolumeM3"] = Math.Round(volumeM3, 2),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["RemovalFraction"] = removalFraction,
            ["OilLoadKgPerDay"] = Math.Round(oilLoadKgPerDay, 2),
            ["OilRemovedKgPerDay"] = Math.Round(oilRemovedKgPerDay, 2),
            ["OilGreaseOutMgL"] = Math.Round(oilOutMgL, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.OilGreaseTrap,
            $"Oil and grease trap {volumeM3:0.0} m3, {length:0.0} x {width:0.0} x {totalDepth:0.0} m, {retentionMinutes:0} min retention",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}
