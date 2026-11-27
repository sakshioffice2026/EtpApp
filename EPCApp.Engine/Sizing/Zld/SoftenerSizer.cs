using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Zld;

public static class SoftenerSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_HARDNESS", "HardnessMgLAsCaCO3"), 800),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_RESIN_CAPACITY", "KgCaCO3PerM3Resin"), 40),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_SERVICE_FLOW", "BedVolumesPerHour"), 15),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_BED_DEPTH", "BedDepthM"), 1.2),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_SALT_DOSE", "KgSaltPerM3Resin"), 120),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_MAX_DIAMETER", "MaxVesselDiameterM"), 2.5),
            design.Rule(DesignBasis.RuleKey("Softener", "SOF_STANDBY", "StandbyUnits"), 1));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double hardnessMgLAsCaCO3 = 800,
        double resinCapacityKgCaCO3PerM3 = 40,
        double serviceBedVolumesPerHour = 15,
        double bedDepthM = 1.2,
        double saltKgPerM3Resin = 120,
        double maxVesselDiameterM = 2.5,
        double standbyUnits = 1)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (resinCapacityKgCaCO3PerM3 <= 0) throw new ArgumentOutOfRangeException(nameof(resinCapacityKgCaCO3PerM3));
        if (serviceBedVolumesPerHour <= 0) throw new ArgumentOutOfRangeException(nameof(serviceBedVolumesPerHour));
        if (bedDepthM <= 0) throw new ArgumentOutOfRangeException(nameof(bedDepthM));

        var flowM3PerHour = flowM3PerDay / 24.0;
        var resinVolumeM3 = flowM3PerHour / serviceBedVolumesPerHour;
        var totalAreaM2 = resinVolumeM3 / bedDepthM;

        var maxAreaM2 = Math.PI / 4.0 * maxVesselDiameterM * maxVesselDiameterM;
        var dutyUnits = Math.Max(1, (int)Math.Ceiling(totalAreaM2 / maxAreaM2));
        var unitAreaM2 = totalAreaM2 / dutyUnits;
        var diameterM = Math.Round(Math.Sqrt(4.0 * unitAreaM2 / Math.PI), 2);
        var totalUnits = dutyUnits + (int)standbyUnits;

        var hardnessLoadKgPerDay = flowM3PerDay * hardnessMgLAsCaCO3 / 1000.0;
        var totalResinM3 = resinVolumeM3;
        var runHours = totalResinM3 * resinCapacityKgCaCO3PerM3 / (hardnessLoadKgPerDay / 24.0);
        var regenerationsPerDay = runHours > 0 ? 24.0 / runHours : 0;
        var saltKgPerDay = hardnessLoadKgPerDay / resinCapacityKgCaCO3PerM3 * saltKgPerM3Resin;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["FlowM3PerHour"] = Math.Round(flowM3PerHour, 2),
            ["HardnessMgLAsCaCO3"] = hardnessMgLAsCaCO3,
            ["HardnessLoadKgPerDay"] = Math.Round(hardnessLoadKgPerDay, 2),
            ["ResinCapacityKgCaCO3PerM3"] = resinCapacityKgCaCO3PerM3,
            ["ServiceBedVolumesPerHour"] = serviceBedVolumesPerHour,
            ["ResinVolumeM3"] = Math.Round(resinVolumeM3, 2),
            ["BedDepthM"] = bedDepthM,
            ["DiameterM"] = diameterM,
            ["DutyUnits"] = dutyUnits,
            ["Units"] = totalUnits,
            ["RunHoursBetweenRegeneration"] = Math.Round(runHours, 1),
            ["RegenerationsPerDay"] = Math.Round(regenerationsPerDay, 2),
            ["SaltKgPerDay"] = Math.Round(saltKgPerDay, 1)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.Softener,
            $"Ion-exchange softener {dutyUnits}W+{(int)standbyUnits}S x {diameterM:0.00} m dia, {resinVolumeM3:0.00} m3 resin (staged sizing)",
            MaterialOfConstruction.Frp,
            parameters);
    }
}