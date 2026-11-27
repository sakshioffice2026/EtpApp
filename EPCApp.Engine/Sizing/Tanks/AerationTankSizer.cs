using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Tanks;

public static class AerationTankSizer
{
    private const double OxygenPerM3Air = 0.28;
    private const double AtmosphericPressurePa = 101325.0;
    private const double WaterPressurePerMPa = 9806.65;
    private const double AdiabaticIndex = 1.4;
    public static SizedEquipment Size(string tag, double flowM3PerDay, double bodInMgL, DesignBasis design) =>
    Size(
        tag,
        flowM3PerDay,
        bodInMgL,
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_BOD_OUT", "BodOutMgL"), 30),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_MLSS", "MlssMgL"), 3500),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_FM", "FoodToMicroorganism"), 0.15),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_DEPTH", "WaterDepthM"), 4.5),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_FREEBOARD", "FreeboardM"), 0.5),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_O2_PER_BOD", "OxygenPerKgBod"), 1.2),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_OTE", "OxygenTransferEfficiency"), 0.20),
        design.Rule(DesignBasis.RuleKey("Aeration", "AT_BLOWER_EFF", "BlowerEfficiency"), 0.65));
    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double bodInMgL,
        double bodOutMgL = 30,
        double mlssMgL = 3500,
        double foodToMicroorganism = 0.15,
        double waterDepthM = 4.5,
        double freeboardM = 0.5,
        double oxygenPerKgBod = 1.2,
        double oxygenTransferEfficiency = 0.20,
        double blowerEfficiency = 0.65)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (bodInMgL <= 0) throw new ArgumentOutOfRangeException(nameof(bodInMgL));
        if (foodToMicroorganism <= 0) throw new ArgumentOutOfRangeException(nameof(foodToMicroorganism));

        var bodLoadKgPerDay = flowM3PerDay * bodInMgL / 1000.0;
        var volume = bodLoadKgPerDay / (foodToMicroorganism * mlssMgL / 1000.0);
        var hrtHours = volume / flowM3PerDay * 24.0;
        var volumetricLoading = bodLoadKgPerDay / volume;

        var (length, width) = TankGeometry.Rectangular(volume, waterDepthM);
        var totalDepth = waterDepthM + freeboardM;

        var oxygenRequiredKgPerDay = flowM3PerDay * Math.Max(bodInMgL - bodOutMgL, 0) / 1000.0 * oxygenPerKgBod;
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
            ["BodInMgL"] = bodInMgL,
            ["BodOutMgL"] = bodOutMgL,
            ["BodLoadKgPerDay"] = Math.Round(bodLoadKgPerDay, 2),
            ["MlssMgL"] = mlssMgL,
            ["FoodToMicroorganism"] = foodToMicroorganism,
            ["VolumeM3"] = Math.Round(volume, 2),
            ["HrtHours"] = Math.Round(hrtHours, 2),
            ["VolumetricLoadingKgBodPerM3PerDay"] = Math.Round(volumetricLoading, 3),
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
            UnitOperationType.Aeration,
            $"Aeration tank {volume:0.0} m3, {length:0.0} x {width:0.0} x {totalDepth:0.0} m",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}