using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Chemical;

public static class PhNeutralizationSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, double inletPh, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            inletPh,
            design.Rule(DesignBasis.RuleKey("PhNeutralization", "PH_TARGET", "TargetPh"), 7.0),
            design.Rule(DesignBasis.RuleKey("PhNeutralization", "PH_RETENTION", "RetentionMinutes"), 15),
            design.Rule(DesignBasis.RuleKey("PhNeutralization", "PH_DOSE", "ReagentKgPerM3PerPhUnit"), 0.4),
            design.Rule(DesignBasis.RuleKey("PhNeutralization", "PH_DEPTH", "WaterDepthM"), 2.5),
            design.Rule(DesignBasis.RuleKey("PhNeutralization", "PH_MIXER_POWER", "MixerKwPerM3"), 0.15));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double inletPh,
        double targetPh = 7.0,
        double retentionMinutes = 15,
        double reagentKgPerM3PerPhUnit = 0.4,
        double waterDepthM = 2.5,
        double mixerKwPerM3 = 0.15)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (retentionMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(retentionMinutes));

        var flowM3PerHour = flowM3PerDay / 24.0;
        var volumeM3 = flowM3PerHour * retentionMinutes / 60.0;
        var (length, width) = TankGeometry.Rectangular(volumeM3, waterDepthM, 1.0);

        var phDeviation = Math.Abs(inletPh - targetPh);
        var reagentKgPerDay = flowM3PerDay * phDeviation * reagentKgPerM3PerPhUnit;
        var reagentIsAlkali = inletPh < targetPh;
        var reagentFlowLPerHour = reagentKgPerDay / 24.0 / 1.2;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["InletPh"] = inletPh,
            ["TargetPh"] = targetPh,
            ["RetentionMinutes"] = retentionMinutes,
            ["VolumeM3"] = Math.Round(volumeM3, 2),
            ["LengthM"] = length,
            ["WidthM"] = width,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = waterDepthM + 0.5,
            ["ReagentIsAlkali"] = reagentIsAlkali ? 1 : 0,
            ["ReagentKgPerDay"] = Math.Round(reagentKgPerDay, 2),
            ["DosingPumpLPerHour"] = Math.Round(reagentFlowLPerHour, 2),
            ["MixerPowerKw"] = Math.Round(volumeM3 * mixerKwPerM3, 2)
        };

        var reagent = reagentIsAlkali ? "alkali (NaOH)" : "acid (H2SO4)";

        return new SizedEquipment(
            tag,
            UnitOperationType.PhNeutralization,
            $"pH neutralization tank {volumeM3:0.0} m3, {reagent} dosing, pH {inletPh:0.0} to {targetPh:0.0}",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}