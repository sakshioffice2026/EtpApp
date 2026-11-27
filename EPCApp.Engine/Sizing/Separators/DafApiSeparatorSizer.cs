using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Separators;

public static class DafApiSeparatorSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, double oilGreaseMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            oilGreaseMgL,
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_SLR", "SurfaceLoadingM3PerM2PerHour"), 6.0),
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_RECYCLE", "RecycleRatio"), 0.20),
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_SATURATOR_PRESSURE", "PressureBarG"), 5.0),
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_DEPTH", "WaterDepthM"), 1.8),
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_FREEBOARD", "FreeboardM"), 0.4),
            design.Rule(DesignBasis.RuleKey("DafApiSeparator", "DAF_OIL_REMOVAL", "RemovalFraction"), 0.85));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double oilGreaseMgL,
        double surfaceLoadingM3PerM2PerHour = 6.0,
        double recycleRatio = 0.20,
        double saturatorPressureBarG = 5.0,
        double waterDepthM = 1.8,
        double freeboardM = 0.4,
        double oilRemovalFraction = 0.85)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (surfaceLoadingM3PerM2PerHour <= 0) throw new ArgumentOutOfRangeException(nameof(surfaceLoadingM3PerM2PerHour));
        if (recycleRatio < 0) throw new ArgumentOutOfRangeException(nameof(recycleRatio));

        var flowM3PerHour = flowM3PerDay / 24.0;
        var areaM2 = flowM3PerHour / surfaceLoadingM3PerM2PerHour;
        var volumeM3 = areaM2 * waterDepthM;
        var (length, width) = TankGeometry.Rectangular(volumeM3, waterDepthM, 3.0);
        var totalDepth = waterDepthM + freeboardM;

        var recycleM3PerHour = flowM3PerHour * recycleRatio;
        var oilRemovedKgPerDay = flowM3PerDay * oilGreaseMgL / 1000.0 * oilRemovalFraction;
        var detentionMinutes = volumeM3 / flowM3PerHour * 60.0;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["FlowM3PerHour"] = Math.Round(flowM3PerHour, 2),
            ["OilGreaseInMgL"] = oilGreaseMgL,
            ["SurfaceLoadingM3PerM2PerHour"] = surfaceLoadingM3PerM2PerHour,
            ["AreaM2"] = Math.Round(areaM2, 2),
            ["VolumeM3"] = Math.Round(volumeM3, 2),
            ["DetentionMinutes"] = Math.Round(detentionMinutes, 1),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["RecycleRatio"] = recycleRatio,
            ["RecycleFlowM3PerHour"] = Math.Round(recycleM3PerHour, 2),
            ["SaturatorPressureBarG"] = saturatorPressureBarG,
            ["OilRemovedKgPerDay"] = Math.Round(oilRemovedKgPerDay, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.DafApiSeparator,
            $"DAF / API separator {areaM2:0.0} m2, {length:0.0} x {width:0.0} x {totalDepth:0.0} m",
            MaterialOfConstruction.Ss304,
            parameters);
    }
}