using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Mechanical;

public static class MechanicalBoq
{
    public static IReadOnlyList<BoqLineItem> Build(IEnumerable<SizedEquipment> equipment, DesignBasis design)
    {
        var lines = new List<BoqLineItem>();

        var pumpHeadM = design.Rule(DesignBasis.RuleKey("Mechanical", "MECH_PUMP_HEAD", "HeadM"), 15);
        var pumpEfficiency = design.Rule(DesignBasis.RuleKey("Mechanical", "MECH_PUMP_EFF", "Efficiency"), 0.60);
        var airPerDiffuser = design.Rule(DesignBasis.RuleKey("Mechanical", "MECH_DIFFUSER_AIR", "AirM3PerHourPerDiffuser"), 3.0);

        var blowerRate = design.Rate("MECH_BLOWER_PER_KW", 28000);
        var diffuserRate = design.Rate("MECH_DIFFUSER_EACH", 3200);
        var pumpRate = design.Rate("MECH_PUMP_PER_KW", 22000);
        var mixerRate = design.Rate("MECH_MIXER_PER_KW", 65000);
        var scraperRate = design.Rate("MECH_SCRAPER_PER_M_DIA", 95000);
        var dosingPumpRate = design.Rate("MECH_DOSING_PUMP_EACH", 48000);
        var chemTankRate = design.Rate("MECH_CHEM_TANK_PER_M3", 22000);
        var mediaRate = design.Rate("MECH_MBBR_MEDIA_PER_M3", 38000);
        var filterPressRate = design.Rate("MECH_FILTER_PRESS_PER_CHAMBER", 85000);
        var skimmerRate = design.Rate("MECH_DAF_SKIMMER_PER_M2", 120000);
        var saturatorRate = design.Rate("MECH_DAF_SATURATOR_EACH", 185000);
        var resinRate = design.Rate("MECH_RESIN_PER_M3", 95000);
        var vesselRate = design.Rate("MECH_FRP_VESSEL_PER_M3", 140000);
        var atfdRate = design.Rate("MECH_ATFD_PER_M2", 1650000);
        var gasHolderRate = design.Rate("MECH_GAS_HOLDER_PER_M3", 18000);

        foreach (var item in equipment)
        {
            var p = item.Parameters;
            var tag = item.Tag;

            var flowM3PerDay = Get(p, "FlowM3PerDay");
            if (flowM3PerDay <= 0) flowM3PerDay = Get(p, "FeedM3PerDay");

            if (flowM3PerDay > 0 && NeedsFeedPump(item.Type))
            {
                var pumpKw = PumpPowerKw(flowM3PerDay / 24.0, pumpHeadM, pumpEfficiency);
                lines.Add(new(tag, "Feed pump set (1 duty + 1 standby), power per pump", "kW", Math.Round(pumpKw * 2, 2), pumpRate, BoqCategory.Mechanical));
            }

            switch (item.Type)
            {
                case UnitOperationType.Equalization:
                    AddAeration(lines, tag, Get(p, "MixingAirM3PerHour"), 0, airPerDiffuser, blowerRate, diffuserRate);
                    break;

                case UnitOperationType.Aeration:
                case UnitOperationType.Mbbr:
                    AddAeration(lines, tag, Get(p, "AirRequiredM3PerHour"), Get(p, "BlowerPowerKw"), airPerDiffuser, blowerRate, diffuserRate);

                    if (item.Type == UnitOperationType.Mbbr)
                    {
                        lines.Add(new(tag, "MBBR carrier media", "m3", Get(p, "MediaVolumeM3"), mediaRate, BoqCategory.Mechanical));
                    }
                    break;

                case UnitOperationType.PrimaryClarifier:
                case UnitOperationType.SecondaryClarifier:
                case UnitOperationType.SludgeThickener:
                    lines.Add(new(tag, "Scraper mechanism with drive", "m dia", Math.Round(Get(p, "DiameterM") * Get(p, "Units"), 2), scraperRate, BoqCategory.Mechanical));
                    break;

                case UnitOperationType.PhNeutralization:
                    lines.Add(new(tag, "Tank mixer", "kW", Get(p, "MixerPowerKw"), mixerRate, BoqCategory.Mechanical));
                    AddDosing(lines, tag, Get(p, "ReagentKgPerDay"), dosingPumpRate, chemTankRate, 1);
                    break;

                case UnitOperationType.CoagulationFlocculation:
                    lines.Add(new(tag, "Rapid mixer", "kW", Get(p, "RapidMixerPowerKw"), mixerRate, BoqCategory.Mechanical));
                    lines.Add(new(tag, "Flocculator slow mixer", "kW", Get(p, "SlowMixerPowerKw"), mixerRate, BoqCategory.Mechanical));
                    AddDosing(lines, tag, Get(p, "CoagulantKgPerDay") + Get(p, "PolymerKgPerDay"), dosingPumpRate, chemTankRate, 2);
                    break;

                case UnitOperationType.DafApiSeparator:
                    lines.Add(new(tag, "Skimmer assembly", "m2", Get(p, "AreaM2"), skimmerRate, BoqCategory.Mechanical));
                    lines.Add(new(tag, "Air saturator with recycle pump", "no.", 1, saturatorRate, BoqCategory.Mechanical));
                    break;

                case UnitOperationType.AnaerobicReactor:
                    lines.Add(new(tag, "Biogas holder", "m3", Math.Round(Get(p, "BiogasM3PerDay") * 0.25, 1), gasHolderRate, BoqCategory.Mechanical));
                    break;

                case UnitOperationType.Disinfection:
                    AddDosing(lines, tag, Get(p, "HypochloriteKgPerDay"), dosingPumpRate, chemTankRate, 1);
                    break;

                case UnitOperationType.FilterPress:
                    lines.Add(new(tag, "Filter press chambers with hydraulic closing", "chamber", Get(p, "Chambers"), filterPressRate, BoqCategory.Mechanical));
                    break;

                case UnitOperationType.Softener:
                    lines.Add(new(tag, "Softener FRP vessel with valves", "m3 resin", Get(p, "ResinVolumeM3") * Get(p, "Units"), vesselRate, BoqCategory.Mechanical));
                    lines.Add(new(tag, "Cation exchange resin", "m3", Get(p, "ResinVolumeM3"), resinRate, BoqCategory.Mechanical));
                    break;

                case UnitOperationType.AtfdCrystallizer:
                    lines.Add(new(tag, "ATFD with crystallizer", "m2", Get(p, "HeatTransferAreaM2"), atfdRate, BoqCategory.Mechanical));
                    break;
            }
        }

        return lines.Where(x => x.Quantity > 0).ToList();
    }

    private static bool NeedsFeedPump(UnitOperationType type) =>
        type is UnitOperationType.Equalization
            or UnitOperationType.DafApiSeparator
            or UnitOperationType.PhNeutralization
            or UnitOperationType.CoagulationFlocculation
            or UnitOperationType.AnaerobicReactor
            or UnitOperationType.Softener
            or UnitOperationType.AtfdCrystallizer
            or UnitOperationType.FilterPress;

    private static void AddAeration(
        List<BoqLineItem> lines,
        string tag,
        double airM3PerHour,
        double blowerKw,
        double airPerDiffuser,
        double blowerRate,
        double diffuserRate)
    {
        if (airM3PerHour <= 0) return;

        var kw = blowerKw > 0 ? blowerKw : airM3PerHour * 0.022;
        var diffusers = Math.Ceiling(airM3PerHour / airPerDiffuser);

        lines.Add(new(tag, "Air blower set (1 duty + 1 standby), power per blower", "kW", Math.Round(kw * 2, 2), blowerRate, BoqCategory.Mechanical));
        lines.Add(new(tag, "Fine bubble diffusers with grid piping", "no.", diffusers, diffuserRate, BoqCategory.Mechanical));
    }

    private static void AddDosing(
        List<BoqLineItem> lines,
        string tag,
        double kgPerDay,
        double dosingPumpRate,
        double chemTankRate,
        int systems)
    {
        if (kgPerDay <= 0) return;

        var tankM3 = Math.Max(0.5, Math.Round(kgPerDay * 7 / 1000.0, 2));

        lines.Add(new(tag, "Dosing pump (1 duty + 1 standby)", "no.", systems * 2, dosingPumpRate, BoqCategory.Mechanical));
        lines.Add(new(tag, "Chemical preparation and storage tank, 7 days", "m3", tankM3 * systems, chemTankRate, BoqCategory.Mechanical));
    }

    private static double PumpPowerKw(double flowM3PerHour, double headM, double efficiency) =>
        flowM3PerHour * headM * 9.81 * 1000.0 / 3600.0 / Math.Max(efficiency, 0.1) / 1000.0 * 1.15;

    private static double Get(IReadOnlyDictionary<string, double> parameters, string key) =>
        parameters.TryGetValue(key, out var value) ? value : 0;
}