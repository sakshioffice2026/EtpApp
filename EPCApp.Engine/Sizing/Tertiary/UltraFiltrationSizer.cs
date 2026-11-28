using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tertiary;

public static class UltraFiltrationSizer
{
    public static SizedEquipment Size(string tag, double feedM3PerDay, DesignBasis design) =>
        Size(
            tag,
            feedM3PerDay,
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_RECOVERY", "RecoveryFraction"), 0.92),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_FLUX", "FluxLmh"), 60),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_OPERATING_HOURS", "HoursPerDay"), 22),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_MODULE_AREA", "AreaM2PerModule"), 40),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_BACKWASH_FLUX", "FluxLmh"), 150),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_BACKWASH_TIME", "SecondsPerCycle"), 60),
            design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_BACKWASH_INTERVAL", "MinutesBetween"), 45));

    public static SizedEquipment Size(
        string tag,
        double feedM3PerDay,
        double recoveryFraction = 0.92,
        double fluxLmh = 60,
        double operatingHoursPerDay = 22,
        double moduleAreaM2 = 40,
        double backwashFluxLmh = 150,
        double backwashSeconds = 60,
        double backwashIntervalMinutes = 45)
    {
        if (feedM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(feedM3PerDay));
        if (fluxLmh <= 0) throw new ArgumentOutOfRangeException(nameof(fluxLmh));
        if (operatingHoursPerDay <= 0 || operatingHoursPerDay > 24) throw new ArgumentOutOfRangeException(nameof(operatingHoursPerDay));
        if (moduleAreaM2 <= 0) throw new ArgumentOutOfRangeException(nameof(moduleAreaM2));
        if (backwashIntervalMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(backwashIntervalMinutes));

        recoveryFraction = Math.Clamp(recoveryFraction, 0.50, 1.0);

        var permeateM3PerDay = feedM3PerDay * recoveryFraction;
        var rejectM3PerDay = feedM3PerDay - permeateM3PerDay;
        var permeateM3PerHour = permeateM3PerDay / operatingHoursPerDay;

        var requiredAreaM2 = permeateM3PerHour * 1000.0 / fluxLmh;
        var modules = Math.Max((int)Math.Ceiling(requiredAreaM2 / moduleAreaM2), 1);
        var installedAreaM2 = modules * moduleAreaM2;
        var designFluxLmh = permeateM3PerHour * 1000.0 / installedAreaM2;

        var backwashFlowM3PerHour = installedAreaM2 * backwashFluxLmh / 1000.0;
        var backwashVolumeM3 = backwashFlowM3PerHour * backwashSeconds / 3600.0;
        var backwashesPerDay = operatingHoursPerDay * 60.0 / backwashIntervalMinutes;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["RecoveryFraction"] = recoveryFraction,
            ["PermeateM3PerDay"] = Math.Round(permeateM3PerDay, 2),
            ["RejectM3PerDay"] = Math.Round(rejectM3PerDay, 2),
            ["OperatingHoursPerDay"] = operatingHoursPerDay,
            ["PermeateM3PerHour"] = Math.Round(permeateM3PerHour, 2),
            ["DesignFluxLmh"] = fluxLmh,
            ["RequiredAreaM2"] = Math.Round(requiredAreaM2, 2),
            ["ModuleAreaM2"] = moduleAreaM2,
            ["Modules"] = modules,
            ["InstalledAreaM2"] = Math.Round(installedAreaM2, 2),
            ["OperatingFluxLmh"] = Math.Round(designFluxLmh, 1),
            ["BackwashFluxLmh"] = backwashFluxLmh,
            ["BackwashFlowM3PerHour"] = Math.Round(backwashFlowM3PerHour, 2),
            ["BackwashVolumeM3"] = Math.Round(backwashVolumeM3, 2),
            ["BackwashesPerDay"] = Math.Round(backwashesPerDay, 1)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.UltraFiltration,
            $"Ultrafiltration skid {modules} modules, {installedAreaM2:0} m2 membrane, {permeateM3PerDay:0.0} m3/day permeate",
            MaterialOfConstruction.MildSteel,
            parameters);
    }
}
