using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Chemical;

public static class ChemicalDosingSizer
{
    public static SizedEquipment Size(string tag, double flowM3PerDay, DesignBasis design) =>
        Size(
            tag,
            flowM3PerDay,
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_DOSE", "DoseMgL"), 20),
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_SOLUTION_STRENGTH", "StrengthFraction"), 0.10),
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_SOLUTION_DENSITY", "DensityKgPerL"), 1.1),
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_STORAGE_DAYS", "StorageDays"), 7),
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_PUMP_MARGIN", "PumpMargin"), 1.25),
            design.Rule(DesignBasis.RuleKey("ChemicalDosing", "CD_TANK_FILL", "TankFillFraction"), 0.85));

    public static SizedEquipment Size(
        string tag,
        double flowM3PerDay,
        double doseMgL = 20,
        double strengthFraction = 0.10,
        double densityKgPerL = 1.1,
        double storageDays = 7,
        double pumpMargin = 1.25,
        double tankFillFraction = 0.85)
    {
        if (flowM3PerDay <= 0) throw new ArgumentOutOfRangeException(nameof(flowM3PerDay));
        if (doseMgL <= 0) throw new ArgumentOutOfRangeException(nameof(doseMgL));
        if (strengthFraction <= 0) throw new ArgumentOutOfRangeException(nameof(strengthFraction));
        if (densityKgPerL <= 0) throw new ArgumentOutOfRangeException(nameof(densityKgPerL));
        if (storageDays <= 0) throw new ArgumentOutOfRangeException(nameof(storageDays));
        if (tankFillFraction <= 0) throw new ArgumentOutOfRangeException(nameof(tankFillFraction));

        var reagentKgPerDay = flowM3PerDay * doseMgL / 1000.0;
        var solutionKgPerDay = reagentKgPerDay / strengthFraction;
        var solutionLPerDay = solutionKgPerDay / densityKgPerL;
        var solutionLPerHour = solutionLPerDay / 24.0;
        var pumpLPerHour = solutionLPerHour * pumpMargin;

        var tankVolumeM3 = solutionLPerDay * storageDays / 1000.0 / tankFillFraction;

        var parameters = new Dictionary<string, double>
        {
            ["FlowM3PerDay"] = Math.Round(flowM3PerDay, 2),
            ["DoseMgL"] = doseMgL,
            ["ReagentKgPerDay"] = Math.Round(reagentKgPerDay, 3),
            ["StrengthFraction"] = strengthFraction,
            ["DensityKgPerL"] = densityKgPerL,
            ["SolutionKgPerDay"] = Math.Round(solutionKgPerDay, 2),
            ["SolutionLPerDay"] = Math.Round(solutionLPerDay, 2),
            ["SolutionLPerHour"] = Math.Round(solutionLPerHour, 2),
            ["PumpMargin"] = pumpMargin,
            ["DosingPumpLPerHour"] = Math.Round(pumpLPerHour, 2),
            ["StorageDays"] = storageDays,
            ["TankFillFraction"] = tankFillFraction,
            ["TankVolumeM3"] = Math.Round(tankVolumeM3, 2)
        };

        return new SizedEquipment(
            tag,
            UnitOperationType.ChemicalDosing,
            $"Chemical dosing system {doseMgL:0.#} mg/L, pump {pumpLPerHour:0.0} L/h, tank {tankVolumeM3:0.00} m3",
            MaterialOfConstruction.Frp,
            parameters);
    }
}
