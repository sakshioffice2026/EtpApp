using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Electrical;

public static class ElectricalBoq
{
    public const string PlantTag = "ELEC";

    public static IReadOnlyList<BoqLineItem> Build(
        IEnumerable<SizedEquipment> equipment,
        IReadOnlyList<BoqLineItem> mechanicalLines,
        DesignBasis design)
    {
        var equipmentList = equipment.ToList();

        var diversity = design.Rule(DesignBasis.RuleKey("Electrical", "ELEC_DIVERSITY", "DiversityFactor"), 0.80);
        var cableMPerMotor = design.Rule(DesignBasis.RuleKey("Electrical", "ELEC_CABLE_LENGTH", "AverageMetersPerMotor"), 40);
        var trayMPerMotor = design.Rule(DesignBasis.RuleKey("Electrical", "ELEC_TRAY_LENGTH", "TrayMetersPerMotor"), 25);
        var earthingPitsPerMotors = design.Rule(DesignBasis.RuleKey("Electrical", "ELEC_EARTH_PITS", "MotorsPerPit"), 6);
        var lightingPerEquipment = design.Rule(DesignBasis.RuleKey("Electrical", "ELEC_LIGHTING", "FixturesPerEquipment"), 3);

        var mccRate = design.Rate("ELEC_MCC_PER_KW", 9500);
        var cableRate = design.Rate("ELEC_CABLE_PER_M", 420);
        var trayRate = design.Rate("ELEC_CABLE_TRAY_PER_M", 750);
        var earthingRate = design.Rate("ELEC_EARTH_PIT_EACH", 18000);
        var lightingRate = design.Rate("ELEC_LIGHT_FIXTURE_EACH", 4800);
        var plcRate = design.Rate("ELEC_PLC_PER_IO", 3200);
        var levelRate = design.Rate("INST_LEVEL_TRANSMITTER_EACH", 42000);
        var flowRate = design.Rate("INST_FLOW_METER_EACH", 85000);
        var phRate = design.Rate("INST_PH_ANALYZER_EACH", 95000);
        var doRate = design.Rate("INST_DO_ANALYZER_EACH", 110000);
        var pressureRate = design.Rate("INST_PRESSURE_TRANSMITTER_EACH", 28000);

        var motorLines = mechanicalLines.Where(x => x.Unit == "kW" && x.Quantity > 0).ToList();
        var connectedKw = motorLines.Sum(x => x.Quantity);
        var motorCount = Math.Max(motorLines.Count, equipmentList.Count);
        var demandKw = connectedKw * diversity;

        var lines = new List<BoqLineItem>();

        if (connectedKw > 0)
        {
            lines.Add(new(PlantTag, "MCC panel with starters and protection, connected load", "kW", Math.Round(connectedKw, 2), mccRate, BoqCategory.Electrical));
            lines.Add(new(PlantTag, "Power and control cables", "m", Math.Round(motorCount * cableMPerMotor, 0), cableRate, BoqCategory.Electrical));
            lines.Add(new(PlantTag, "Cable trays with supports", "m", Math.Round(motorCount * trayMPerMotor, 0), trayRate, BoqCategory.Electrical));
            lines.Add(new(PlantTag, "Earthing pits", "no.", Math.Ceiling(motorCount / Math.Max(earthingPitsPerMotors, 1)), earthingRate, BoqCategory.Electrical));
        }

        lines.Add(new(PlantTag, "Lighting fixtures with wiring", "no.", Math.Round(equipmentList.Count * lightingPerEquipment, 0), lightingRate, BoqCategory.Electrical));

        var levelCount = 0;
        var flowCount = 0;
        var phCount = 0;
        var doCount = 0;
        var pressureCount = 0;

        foreach (var item in equipmentList)
        {
            switch (item.Type)
            {
                case UnitOperationType.ScreenChamber:
                    levelCount += 1;
                    break;
                case UnitOperationType.OilGreaseTrap:
                    levelCount += 1;
                    break;
                case UnitOperationType.ChemicalDosing:
                    levelCount += 1;
                    flowCount += 1;
                    break;
                case UnitOperationType.Equalization:
                    levelCount += 1;
                    flowCount += 1;
                    break;
                case UnitOperationType.PhNeutralization:
                    phCount += 2;
                    levelCount += 1;
                    break;
                case UnitOperationType.CoagulationFlocculation:
                    levelCount += 1;
                    break;
                case UnitOperationType.Aeration:
                case UnitOperationType.Mbbr:
                    doCount += 1;
                    pressureCount += 1;
                    break;
                case UnitOperationType.AnaerobicReactor:
                    phCount += 1;
                    pressureCount += 1;
                    break;
                case UnitOperationType.Disinfection:
                    flowCount += 1;
                    break;
                case UnitOperationType.FilterPress:
                    pressureCount += 1;
                    break;
                case UnitOperationType.PressureSandFilter:
                case UnitOperationType.ActivatedCarbonFilter:
                case UnitOperationType.UltraFiltration:
                    flowCount += 1;
                    pressureCount += 2;
                    break;
                case UnitOperationType.ReverseOsmosis:
                    flowCount += 2;
                    pressureCount += 3;
                    phCount += 1;
                    break;
                case UnitOperationType.MultiEffectEvaporator:
                    levelCount += 1;
                    flowCount += 1;
                    pressureCount += 1;
                    break;
                case UnitOperationType.Softener:
                case UnitOperationType.AtfdCrystallizer:
                    flowCount += 1;
                    pressureCount += 1;
                    break;
            }
        }

        AddInstrument(lines, "Level transmitter", levelCount, levelRate);
        AddInstrument(lines, "Electromagnetic flow meter", flowCount, flowRate);
        AddInstrument(lines, "pH analyzer with probe", phCount, phRate);
        AddInstrument(lines, "Dissolved oxygen analyzer", doCount, doRate);
        AddInstrument(lines, "Pressure transmitter", pressureCount, pressureRate);

        var ioPoints = motorLines.Count * 3 + levelCount + flowCount + phCount + doCount + pressureCount;
        if (ioPoints > 0)
        {
            lines.Add(new(PlantTag, "PLC with HMI, I/O points", "no.", ioPoints, plcRate, BoqCategory.Electrical));
        }

        return lines.Where(x => x.Quantity > 0).ToList();
    }

    private static void AddInstrument(List<BoqLineItem> lines, string item, int count, double rate)
    {
        if (count > 0)
        {
            lines.Add(new(PlantTag, item, "no.", count, rate, BoqCategory.Electrical));
        }
    }
}