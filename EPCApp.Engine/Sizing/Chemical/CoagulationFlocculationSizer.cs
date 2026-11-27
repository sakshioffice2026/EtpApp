using EPCApp.Database.Enums;
using EPCApp.Engine.Civil;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Chemical;

public static class CoagulationFlocculationSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, double tssMgL, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            tssMgL,
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_COAG_RETENTION", "CoagulationMinutes"), 2),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_FLOC_RETENTION", "FlocculationMinutes"), 20),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_COAG_DOSE", "CoagulantMgL"), 150),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_POLY_DOSE", "PolymerMgL"), 2),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_DEPTH", "WaterDepthM"), 2.5),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_RAPID_MIX", "RapidMixKwPerM3"), 1.0),
            design.Rule(DesignBasis.RuleKey("CoagulationFlocculation", "CF_SLOW_MIX", "SlowMixKwPerM3"), 0.05));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double tssMgL,
        double coagulationMinutes = 2,
        double flocculationMinutes = 20,
        double coagulantMgL = 150,
        double polymerMgL = 2,
        double waterDepthM = 2.5,
        double rapidMixKwPerM3 = 1.0,
        double slowMixKwPerM3 = 0.05)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (coagulationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(coagulationMinutes));
        if (flocculationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(flocculationMinutes));

        var flowM3PerHour = flowM3PerDay / 24.0;
        var coagVolumeM3 = flowM3PerHour * coagulationMinutes / 60.0;
        var floccVolumeM3 = flowM3PerHour * flocculationMinutes / 60.0;

        var (coagLength, coagWidth) = TankGeometry.Rectangular(coagVolumeM3, waterDepthM, 1.0);
        var (floccLength, floccWidth) = TankGeometry.Rectangular(floccVolumeM3, waterDepthM, 1.0);

        var coagulantKgPerDay = flowM3PerDay * coagulantMgL / 1000.0;
        var polymerKgPerDay = flowM3PerDay * polymerMgL / 1000.0;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["TssInMgL"] = tssMgL,
            ["CoagulationMinutes"] = coagulationMinutes,
            ["FlocculationMinutes"] = flocculationMinutes,
            ["CoagulationVolumeM3"] = Math.Round(coagVolumeM3, 2),
            ["FlocculationVolumeM3"] = Math.Round(floccVolumeM3, 2),
            ["VolumeM3"] = Math.Round(coagVolumeM3 + floccVolumeM3, 2),
            ["CoagulationLengthM"] = coagLength,
            ["CoagulationWidthM"] = coagWidth,
            ["FlocculationLengthM"] = floccLength,
            ["FlocculationWidthM"] = floccWidth,
            ["WaterDepthM"] = waterDepthM,
            ["DepthM"] = waterDepthM + 0.5,
            ["CoagulantMgL"] = coagulantMgL,
            ["CoagulantKgPerDay"] = Math.Round(coagulantKgPerDay, 2),
            ["PolymerMgL"] = polymerMgL,
            ["PolymerKgPerDay"] = Math.Round(polymerKgPerDay, 3),
            ["RapidMixerPowerKw"] = Math.Round(coagVolumeM3 * rapidMixKwPerM3, 2),
            ["SlowMixerPowerKw"] = Math.Round(floccVolumeM3 * slowMixKwPerM3, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.CoagulationFlocculation,
            $"Coagulation {coagVolumeM3:0.0} m3 + flocculation {floccVolumeM3:0.0} m3 with chemical dosing",
            MaterialOfConstruction.RccM30,
            parameters);
    }
}