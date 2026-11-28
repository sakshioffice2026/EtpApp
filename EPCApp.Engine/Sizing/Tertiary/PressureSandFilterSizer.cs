using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tertiary;

public static class PressureSandFilterSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_OPERATING_HOURS", "HoursPerDay"), 20),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_FILTRATION_RATE", "RateMPerHour"), 10),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_MAX_DIAMETER", "MaxVesselDiameterM"), 2.5),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_BED_DEPTH", "MediaDepthM"), 1.0),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_BACKWASH_RATE", "RateMPerHour"), 30),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_BACKWASH_TIME", "Minutes"), 10),
            design.Rule(DesignBasis.RuleKey("PressureSandFilter", "PSF_STANDBY", "StandbyUnits"), 1));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double operatingHoursPerDay = 20,
        double filtrationRateMPerHour = 10,
        double maxVesselDiameterM = 2.5,
        double mediaDepthM = 1.0,
        double backwashRateMPerHour = 30,
        double backwashMinutes = 10,
        double standbyUnits = 1)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (filtrationRateMPerHour <= 0) throw new ArgumentOutOfRangeException(nameof(filtrationRateMPerHour));
        if (maxVesselDiameterM <= 0) throw new ArgumentOutOfRangeException(nameof(maxVesselDiameterM));
        if (mediaDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(mediaDepthM));

        var flowM3PerHour = flowM3PerDay / operatingHoursPerDay;
        var totalAreaM2 = flowM3PerHour / filtrationRateMPerHour;

        var maxAreaM2 = Math.PI * maxVesselDiameterM * maxVesselDiameterM / 4.0;
        var dutyUnits = Math.Max((int)Math.Ceiling(totalAreaM2 / maxAreaM2), 1);
        var areaPerUnitM2 = totalAreaM2 / dutyUnits;
        var diameterM = Math.Ceiling(Math.Sqrt(4.0 * areaPerUnitM2 / Math.PI) * 20.0) / 20.0;
        var installedAreaPerUnitM2 = Math.PI * diameterM * diameterM / 4.0;

        var totalUnits = dutyUnits + (int)standbyUnits;
        var mediaVolumeM3 = installedAreaPerUnitM2 * mediaDepthM * totalUnits;

        var backwashFlowM3PerHour = installedAreaPerUnitM2 * backwashRateMPerHour;
        var backwashVolumeM3 = backwashFlowM3PerHour * backwashMinutes / 60.0;

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
            ["MediaDepthM"] = mediaDepthM,
            ["MediaVolumeM3"] = Math.Round(mediaVolumeM3, 2),
            ["BackwashRateMPerHour"] = backwashRateMPerHour,
            ["BackwashFlowM3PerHour"] = Math.Round(backwashFlowM3PerHour, 2),
            ["BackwashMinutes"] = backwashMinutes,
            ["BackwashVolumeM3"] = Math.Round(backwashVolumeM3, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.PressureSandFilter,
            $"Pressure sand filter {dutyUnits} duty + {standbyUnits:0} standby, {diameterM:0.00} m dia each, {filtrationRateMPerHour:0.#} m/h",
            MaterialOfConstruction.MildSteel,
            parameters);
    }
}
