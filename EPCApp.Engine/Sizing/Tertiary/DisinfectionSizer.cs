using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tertiary;

public static class DisinfectionSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_CONTACT_TIME", "ContactMinutes"), 30),
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_CHLORINE_DOSE", "ChlorineMgL"), 5),
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_HYPO_STRENGTH", "HypochloriteFraction"), 0.10),
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_DEPTH", "WaterDepthM"), 2.0),
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_FREEBOARD", "FreeboardM"), 0.5),
            design.Rule(DesignBasis.RuleKey("Disinfection", "DIS_PEAK_FACTOR", "PeakFactor"), 1.5));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double contactMinutes = 30,
        double chlorineMgL = 5,
        double hypochloriteFraction = 0.10,
        double waterDepthM = 2.0,
        double freeboardM = 0.5,
        double peakFactor = 1.5)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (contactMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(contactMinutes));
        if (hypochloriteFraction <= 0) throw new ArgumentOutOfRangeException(nameof(hypochloriteFraction));
        if (peakFactor < 1) throw new ArgumentOutOfRangeException(nameof(peakFactor));

        var peakFlowM3PerHour = flowM3PerDay / 24.0 * peakFactor;
        var volumeM3 = peakFlowM3PerHour * contactMinutes / 60.0;
        var (length, width) = TankGeometry.Rectangular(volumeM3, waterDepthM, 4.0);
        var totalDepth = waterDepthM + freeboardM;

        var chlorineKgPerDay = flowM3PerDay * chlorineMgL / 1000.0;
        var hypochloriteKgPerDay = chlorineKgPerDay / hypochloriteFraction;
        var hypochloriteLPerHour = hypochloriteKgPerDay / 24.0 / 1.1;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["PeakFactor"] = peakFactor,
            ["PeakFlowM3PerHour"] = Math.Round(peakFlowM3PerHour, 2),
            ["ContactMinutes"] = contactMinutes,
            ["VolumeM3"] = Math.Round(volumeM3, 2),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["ChlorineDoseMgL"] = chlorineMgL,
            ["ChlorineKgPerDay"] = Math.Round(chlorineKgPerDay, 3),
            ["HypochloriteKgPerDay"] = Math.Round(hypochloriteKgPerDay, 2),
            ["DosingPumpLPerHour"] = Math.Round(hypochloriteLPerHour, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.Disinfection,
            $"Chlorine contact tank {volumeM3:0.0} m3, {length:0.0} x {width:0.0} x {totalDepth:0.0} m, {contactMinutes:0} min contact",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}