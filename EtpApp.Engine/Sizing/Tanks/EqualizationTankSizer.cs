using EtpApp.Database.Enums;
using EtpApp.Engine.Civil;
using EtpApp.Engine.Models;

namespace EtpApp.Engine.Sizing.Tanks;

public static class EqualizationTankSizer
{
    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double hrtHours = 8,
        double waterDepthM = 4.0,
        double freeboardM = 0.5,
        double mixingAirM3PerHourPerM3 = 0.75)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (hrtHours <= 0) throw new ArgumentOutOfRangeException(nameof(hrtHours));

        var volume = flowM3PerDay * hrtHours / 24.0;
        var (length, width) = TankGeometry.Rectangular(volume, waterDepthM);
        var totalDepth = waterDepthM + freeboardM;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["HrtHours"] = hrtHours,
            ["VolumeM3"] = Math.Round(volume, 2),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["MixingAirM3PerHour"] = Math.Round(volume * mixingAirM3PerHourPerM3, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.Equalization,
            $"Equalization tank {volume:0.0} m3, {length:0.0} x {width:0.0} x {totalDepth:0.0} m",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}