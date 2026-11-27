using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Biology;

public static class MbbrSizer
{
    private const double OxygenPerM3Air = 0.28;
    private const double AtmosphericPressurePa = 101325.0;
    private const double WaterPressurePerMPa = 9806.65;
    private const double AdiabaticIndex = 1.4;

    public static SizedEquipment Size(string tag, double flowM3PerDay, double codInMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            codInMgL,
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_COD_OUT", "CodOutMgL"), 100),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_SALR", "SurfaceLoadGCodPerM2PerDay"), 12),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_MEDIA_SSA", "MediaSurfaceAreaM2PerM3"), 500),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_FILL", "MediaFillFraction"), 0.50),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_DEPTH", "WaterDepthM"), 4.0),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_FREEBOARD", "FreeboardM"), 0.5),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_O2_PER_COD", "OxygenPerKgCod"), 1.0),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_OTE", "OxygenTransferEfficiency"), 0.18),
            design.Rule(DesignBasis.RuleKey("Mbbr", "MBBR_BLOWER_EFF", "BlowerEfficiency"), 0.65));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double codInMgL,
        double codOutMgL = 100,
        double surfaceLoadGCodPerM2PerDay = 12,
        double mediaSurfaceAreaM2PerM3 = 500,
        double mediaFillFraction = 0.50,
        double waterDepthM = 4.0,
        double freeboardM = 0.5,
        double oxygenPerKgCod = 1.0,
        double oxygenTransferEfficiency = 0.18,
        double blowerEfficiency = 0.65)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (codInMgL <= 0) throw new ArgumentOutOfRangeException(nameof(codInMgL));
        if (surfaceLoadGCodPerM2PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(surfaceLoadGCodPerM2PerDay));
        if (mediaSurfaceAreaM2PerM3 <= 0) throw new ArgumentOutOfRangeException(nameof(mediaSurfaceAreaM2PerM3));
        if (mediaFillFraction <= 0 || mediaFillFraction > 0.67) throw new ArgumentOutOfRangeException(nameof(mediaFillFraction));

        var codRemovedKgPerDay = flowM3PerDay * Math.Max(codInMgL - codOutMgL, 0) / 1000.0;
        var requiredMediaAreaM2 = codRemovedKgPerDay * 1000.0 / surfaceLoadGCodPerM2PerDay;
        var mediaVolumeM3 = requiredMediaAreaM2 / mediaSurfaceAreaM2PerM3;
        var reactorVolumeM3 = mediaVolumeM3 / mediaFillFraction;
        var hrtHours = reactorVolumeM3 / flowM3PerDay * 24.0;

        var (length, width) = TankGeometry.Rectangular(reactorVolumeM3, waterDepthM);
        var totalDepth = waterDepthM + freeboardM;

        var oxygenRequiredKgPerDay = codRemovedKgPerDay * oxygenPerKgCod;
        var airM3PerHour = oxygenRequiredKgPerDay / (OxygenPerM3Air * oxygenTransferEfficiency) / 24.0;

        var submergenceM = waterDepthM + 0.5;
        var dischargePressurePa = AtmosphericPressurePa + WaterPressurePerMPa * submergenceM;
        var blowerPowerKw = airM3PerHour / 3600.0 * AtmosphericPressurePa
                            * (AdiabaticIndex / (AdiabaticIndex - 1.0))
                            * (Math.Pow(dischargePressurePa / AtmosphericPressurePa, (AdiabaticIndex - 1.0) / AdiabaticIndex) - 1.0)
                            / blowerEfficiency / 1000.0;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["CodInMgL"] = codInMgL,
            ["CodOutMgL"] = codOutMgL,
            ["CodRemovedKgPerDay"] = Math.Round(codRemovedKgPerDay, 2),
            ["SurfaceLoadGCodPerM2PerDay"] = surfaceLoadGCodPerM2PerDay,
            ["MediaSurfaceAreaM2PerM3"] = mediaSurfaceAreaM2PerM3,
            ["MediaFillFraction"] = mediaFillFraction,
            ["MediaAreaM2"] = Math.Round(requiredMediaAreaM2, 0),
            ["MediaVolumeM3"] = Math.Round(mediaVolumeM3, 2),
            ["VolumeM3"] = Math.Round(reactorVolumeM3, 2),
            ["HrtHours"] = Math.Round(hrtHours, 2),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["OxygenRequiredKgPerDay"] = Math.Round(oxygenRequiredKgPerDay, 2),
            ["AirRequiredM3PerHour"] = Math.Round(airM3PerHour, 1),
            ["BlowerPowerKw"] = Math.Round(blowerPowerKw, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.Mbbr,
            $"MBBR reactor {reactorVolumeM3:0.0} m3 with {mediaVolumeM3:0.0} m3 media, {length:0.0} x {width:0.0} x {totalDepth:0.0} m",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}