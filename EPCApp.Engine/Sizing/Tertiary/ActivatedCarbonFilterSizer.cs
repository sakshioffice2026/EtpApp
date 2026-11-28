using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tertiary;

public static class ActivatedCarbonFilterSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_OPERATING_HOURS", "HoursPerDay"), 20),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_FILTRATION_RATE", "RateMPerHour"), 8),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_MAX_DIAMETER", "MaxVesselDiameterM"), 2.5),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_BED_DEPTH", "MediaDepthM"), 1.2),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_EBCT", "MinMinutes"), 10),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_CARBON_DENSITY", "KgPerM3"), 450),
            design.Rule(DesignBasis.RuleKey("ActivatedCarbonFilter", "ACF_STANDBY", "StandbyUnits"), 1));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double operatingHoursPerDay = 20,
        double filtrationRateMPerHour = 8,
        double maxVesselDiameterM = 2.5,
        double mediaDepthM = 1.2,
        double minEbctMinutes = 10,
        double carbonDensityKgPerM3 = 450,
        double standbyUnits = 1)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (filtrationRateMPerHour <= 0) throw new ArgumentOutOfRangeException(nameof(filtrationRateMPerHour));
        if (maxVesselDiameterM <= 0) throw new ArgumentOutOfRangeException(nameof(maxVesselDiameterM));
        if (mediaDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(mediaDepthM));
        if (minEbctMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(minEbctMinutes));

        var flowM3PerHour = flowM3PerDay / operatingHoursPerDay;
        var totalAreaM2 = flowM3PerHour / filtrationRateMPerHour;

        var maxAreaM2 = Math.PI * maxVesselDiameterM * maxVesselDiameterM / 4.0;
        var dutyUnits = Math.Max((int)Math.Ceiling(totalAreaM2 / maxAreaM2), 1);
        var areaPerUnitM2 = totalAreaM2 / dutyUnits;
        var diameterM = Math.Ceiling(Math.Sqrt(4.0 * areaPerUnitM2 / Math.PI) * 20.0) / 20.0;
        var installedAreaPerUnitM2 = Math.PI * diameterM * diameterM / 4.0;

        var requiredBedVolumeM3 = flowM3PerHour * minEbctMinutes / 60.0;
        var bedVolumeDutyM3 = installedAreaPerUnitM2 * mediaDepthM * dutyUnits;
        var ebctMinutes = bedVolumeDutyM3 / flowM3PerHour * 60.0;

        if (ebctMinutes < minEbctMinutes)
        {
            mediaDepthM = Math.Ceiling(requiredBedVolumeM3 / (installedAreaPerUnitM2 * dutyUnits) * 20.0) / 20.0;
            bedVolumeDutyM3 = installedAreaPerUnitM2 * mediaDepthM * dutyUnits;
            ebctMinutes = bedVolumeDutyM3 / flowM3PerHour * 60.0;
        }

        var totalUnits = dutyUnits + (int)standbyUnits;
        var carbonVolumeM3 = installedAreaPerUnitM2 * mediaDepthM * totalUnits;
        var carbonKg = carbonVolumeM3 * carbonDensityKgPerM3;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["OperatingHoursPerDay"] = operatingHoursPerDay,
            ["FlowM3PerHour"] = Math.Round(flowM3PerHour, 2),
            ["FiltrationRateMPerHour"] = filtrationRateMPerHour,
            ["RequiredAreaM2"] = Math.Round(totalAreaM2, 2),
            ["DutyUnits"] = dutyUnits,
            ["StandbyUnits"] = standbyUnits,
            ["Units"] = totalUnits,
            ["VesselDiameterM"] = Math.Round(diameterM, 2),
            ["AreaPerUnitM2"] = Math.Round(installedAreaPerUnitM2, 2),
            ["MinEbctMinutes"] = minEbctMinutes,
            ["MediaDepthM"] = Math.Round(mediaDepthM, 2),
            ["EbctMinutes"] = Math.Round(ebctMinutes, 1),
            ["MediaVolumeM3"] = Math.Round(carbonVolumeM3, 2),
            ["CarbonDensityKgPerM3"] = carbonDensityKgPerM3,
            ["CarbonKg"] = Math.Round(carbonKg, 0)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.ActivatedCarbonFilter,
            $"Activated carbon filter {dutyUnits} duty + {standbyUnits:0} standby, {diameterM:0.00} m dia each, {ebctMinutes:0.#} min EBCT",
            MaterialOfConstruction.MildSteel,
            parameters);
    }
}
