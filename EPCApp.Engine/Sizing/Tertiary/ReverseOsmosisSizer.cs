using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tertiary;

public static class ReverseOsmosisSizer
{
    public static SizedEquipment Size(string tag, double feedM3PerDay, double feedTdsMgL, DesignBasis design) =>
        Size(
            tag,
            feedM3PerDay,
            feedTdsMgL,
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_RECOVERY", "RecoveryFraction"), 0.75),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_SALT_REJECTION", "RejectionFraction"), 0.98),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_FLUX", "FluxLmh"), 18),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_OPERATING_HOURS", "HoursPerDay"), 22),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_ELEMENT_AREA", "AreaM2PerElement"), 37),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_ELEMENTS_PER_VESSEL", "Elements"), 6),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_NET_DRIVING_PRESSURE", "NdpBar"), 10),
            design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_PUMP_EFFICIENCY", "Efficiency"), 0.70));

    public static SizedEquipment Size(
        string tag,
        double feedM3PerDay,
        double feedTdsMgL,
        double recoveryFraction = 0.75,
        double rejectionFraction = 0.98,
        double fluxLmh = 18,
        double operatingHoursPerDay = 22,
        double elementAreaM2 = 37,
        double elementsPerVessel = 6,
        double netDrivingPressureBar = 10,
        double pumpEfficiency = 0.70)
    {
        if (feedM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(feedM3PerDay));
        if (fluxLmh <= 0) throw new ArgumentOutOfRangeException(nameof(fluxLmh));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (elementAreaM2 <= 0) throw new ArgumentOutOfRangeException(nameof(elementAreaM2));
        if (elementsPerVessel < 1) throw new ArgumentOutOfRangeException(nameof(elementsPerVessel));
        if (pumpEfficiency <= 0 || pumpEfficiency > 1) throw new ArgumentOutOfRangeException(nameof(pumpEfficiency));

        recoveryFraction = Math.Clamp(recoveryFraction, 0.10, 0.95);
        rejectionFraction = Math.Clamp(rejectionFraction, 0.50, 0.999);

        var permeateM3PerDay = feedM3PerDay * recoveryFraction;
        var rejectM3PerDay = feedM3PerDay - permeateM3PerDay;

        var permeateTdsMgL = feedTdsMgL * (1.0 - rejectionFraction);
        var rejectTdsMgL = rejectM3PerDay > 0
            ? (feedTdsMgL * feedM3PerDay - permeateTdsMgL * permeateM3PerDay) / rejectM3PerDay
            : feedTdsMgL;

        var feedM3PerHour = feedM3PerDay / operatingHoursPerDay;
        var permeateM3PerHour = permeateM3PerDay / operatingHoursPerDay;

        var requiredAreaM2 = permeateM3PerHour * 1000.0 / fluxLmh;
        var elements = Math.Max((int)Math.Ceiling(requiredAreaM2 / elementAreaM2), 1);
        var vessels = Math.Max((int)Math.Ceiling(elements / elementsPerVessel), 1);
        var installedElements = vessels * (int)elementsPerVessel;
        var installedAreaM2 = installedElements * elementAreaM2;
        var operatingFluxLmh = permeateM3PerHour * 1000.0 / installedAreaM2;

        var averageFeedConcMgL = (feedTdsMgL + rejectTdsMgL) / 2.0;
        var osmoticPressureBar = averageFeedConcMgL * 0.00077;
        var feedPressureBar = osmoticPressureBar + netDrivingPressureBar;
        var pumpPowerKw = feedM3PerHour * feedPressureBar / (36.0 * pumpEfficiency);

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["FeedTdsMgL"] = Math.Round(feedTdsMgL, 1),
            ["RecoveryFraction"] = recoveryFraction,
            ["RejectionFraction"] = rejectionFraction,
            ["PermeateM3PerDay"] = Math.Round(permeateM3PerDay, 2),
            ["RejectM3PerDay"] = Math.Round(rejectM3PerDay, 2),
            ["PermeateTdsMgL"] = Math.Round(permeateTdsMgL, 1),
            ["RejectTdsMgL"] = Math.Round(rejectTdsMgL, 1),
            ["OperatingHoursPerDay"] = operatingHoursPerDay,
            ["FeedM3PerHour"] = Math.Round(feedM3PerHour, 2),
            ["PermeateM3PerHour"] = Math.Round(permeateM3PerHour, 2),
            ["DesignFluxLmh"] = fluxLmh,
            ["RequiredAreaM2"] = Math.Round(requiredAreaM2, 2),
            ["ElementAreaM2"] = elementAreaM2,
            ["ElementsPerVessel"] = elementsPerVessel,
            ["Vessels"] = vessels,
            ["Elements"] = installedElements,
            ["InstalledAreaM2"] = Math.Round(installedAreaM2, 2),
            ["OperatingFluxLmh"] = Math.Round(operatingFluxLmh, 1),
            ["OsmoticPressureBar"] = Math.Round(osmoticPressureBar, 1),
            ["NetDrivingPressureBar"] = netDrivingPressureBar,
            ["FeedPressureBar"] = Math.Round(feedPressureBar, 1),
            ["PumpEfficiency"] = pumpEfficiency,
            ["HighPressurePumpKw"] = Math.Round(pumpPowerKw, 1)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.ReverseOsmosis,
            $"Reverse osmosis skid {vessels} vessels, {installedElements} elements, {permeateM3PerDay:0.0} m3/day permeate at {recoveryFraction * 100:0}% recovery",
            MaterialOfConstruction.Ss316,
            parameters);
    }
}
