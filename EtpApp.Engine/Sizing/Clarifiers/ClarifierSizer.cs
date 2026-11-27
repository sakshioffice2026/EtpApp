using EtpApp.Database.Enums;
using EtpApp.Engine.Civil;
using EtpApp.Engine.Models;

namespace EtpApp.Engine.Sizing.Clarifiers;

public static class ClarifierSizer
{
    private const double MaxDiameterM = 30.0;

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double mlssMgL = 3500,
        double recycleRatio = 0.75,
        double surfaceOverflowRate = 20,
        double waterDepthM = 3.5,
        double freeboardM = 0.5)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (surfaceOverflowRate <= 0) throw new ArgumentOutOfRangeException(nameof(surfaceOverflowRate));

        var totalAreaM2 = flowM3PerDay / surfaceOverflowRate;
        var maxUnitAreaM2 = Math.PI / 4.0 * MaxDiameterM * MaxDiameterM;
        var units = Math.Max(1, (int)Math.Ceiling(totalAreaM2 / maxUnitAreaM2));
        var unitAreaM2 = totalAreaM2 / units;
        var diameterM = TankGeometry.CircularDiameter(unitAreaM2);

        var solidsLoading = flowM3PerDay * (1.0 + recycleRatio) / 24.0 * (mlssMgL / 1000.0) / totalAreaM2;
        var totalDepth = waterDepthM + freeboardM;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["SurfaceOverflowRateM3PerM2PerDay"] = surfaceOverflowRate,
            ["TotalAreaM2"] = Math.Round(totalAreaM2, 2),
            ["Units"] = units,
            ["DiameterM"] = diameterM,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = totalDepth,
            ["RecycleRatio"] = recycleRatio,
            ["SolidsLoadingKgPerM2PerHour"] = Math.Round(solidsLoading, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.SecondaryClarifier,
            $"Secondary clarifier {units} no. x {diameterM:0.0} m dia x {totalDepth:0.0} m depth",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}