using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Preliminary;

public static class ScreenChamberSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_PEAK_FACTOR", "PeakFactor"), 2.5),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_VELOCITY", "ApproachVelocityMPerSec"), 0.6),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_DEPTH", "WaterDepthM"), 0.5),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_LENGTH", "ChannelLengthM"), 3.0),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_FREEBOARD", "FreeboardM"), 0.3),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_BAR_SPACING", "ClearSpacingMm"), 20),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_BAR_THICKNESS", "BarThicknessMm"), 10),
            design.Rule(DesignBasis.RuleKey("ScreenChamber", "SCR_SCREENINGS", "M3PerThousandM3"), 0.03));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double peakFactor = 2.5,
        double approachVelocityMPerSec = 0.6,
        double waterDepthM = 0.5,
        double channelLengthM = 3.0,
        double freeboardM = 0.3,
        double clearSpacingMm = 20,
        double barThicknessMm = 10,
        double screeningsM3PerThousandM3 = 0.03)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (peakFactor < 1) throw new ArgumentOutOfRangeException(nameof(peakFactor));
        if (approachVelocityMPerSec <= 0) throw new ArgumentOutOfRangeException(nameof(approachVelocityMPerSec));
        if (waterDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(waterDepthM));
        if (clearSpacingMm <= 0) throw new ArgumentOutOfRangeException(nameof(clearSpacingMm));
        if (barThicknessMm <= 0) throw new ArgumentOutOfRangeException(nameof(barThicknessMm));

        var peakFlowM3PerSec = flowM3PerDay / 86400.0 * peakFactor;
        var flowAreaM2 = peakFlowM3PerSec / approachVelocityMPerSec;
        var channelWidthM = Math.Max(Math.Ceiling(flowAreaM2 / waterDepthM * 20.0) / 20.0, 0.5);
        var totalDepthM = waterDepthM + freeboardM;

        var barCount = Math.Max((int)Math.Floor((channelWidthM * 1000.0 - clearSpacingMm) / (clearSpacingMm + barThicknessMm)), 1);
        var screeningsM3PerDay = flowM3PerDay * screeningsM3PerThousandM3 / 1000.0;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["PeakFactor"] = peakFactor,
            ["PeakFlowM3PerSec"] = Math.Round(peakFlowM3PerSec, 4),
            ["ApproachVelocityMPerSec"] = approachVelocityMPerSec,
            ["FlowAreaM2"] = Math.Round(flowAreaM2, 3),
            ["WaterDepthM"] = waterDepthM,
            ["LengthM"] = channelLengthM,
            ["WidthM"] = Math.Round(channelWidthM, 2),
            ["DepthM"] = totalDepthM,
            ["ClearSpacingMm"] = clearSpacingMm,
            ["BarThicknessMm"] = barThicknessMm,
            ["BarCount"] = barCount,
            ["ScreeningsM3PerDay"] = Math.Round(screeningsM3PerDay, 4)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.ScreenChamber,
            $"Screen chamber {channelLengthM:0.0} x {channelWidthM:0.00} x {totalDepthM:0.0} m, {clearSpacingMm:0} mm bar screen",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}
