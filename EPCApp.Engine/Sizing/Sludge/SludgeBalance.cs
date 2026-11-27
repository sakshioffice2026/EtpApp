using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Sludge;

public static class SludgeBalance
{
    public static void Compute(
        IEnumerable<SizedEquipment> equipment,
        StreamBasis basis,
        DesignBasis design,
        TrainPlan plan)
    {
        var list = equipment.ToList();

        var bioYieldBod = design.Rule(DesignBasis.RuleKey("Sludge", "SL_YIELD_AERATION", "KgTssPerKgBodRemoved"), 0.40);
        var bioYieldCodMbbr = design.Rule(DesignBasis.RuleKey("Sludge", "SL_YIELD_MBBR", "KgTssPerKgCodRemoved"), 0.30);
        var bioYieldCodAnaerobic = design.Rule(DesignBasis.RuleKey("Sludge", "SL_YIELD_ANAEROBIC", "KgTssPerKgCodRemoved"), 0.05);
        var chemicalSludgeFactor = design.Rule(DesignBasis.RuleKey("Sludge", "SL_CHEM_FACTOR", "KgSludgePerKgCoagulant"), 0.50);
        var coagTssRemoval = design.Rule(DesignBasis.RuleKey("Sludge", "SL_COAG_TSS_REMOVAL", "RemovalFraction"), 0.80);
        var feedSolidsFraction = design.Rule(DesignBasis.RuleKey("Sludge", "SL_FEED_SOLIDS", "SolidsFraction"), 0.008);

        double dryKgPerDay = 0;
        var hasPrimary = false;

        foreach (var item in list)
        {
            var p = item.Parameters;

            switch (item.Type)
            {
                case UnitOperationType.PrimaryClarifier:
                    hasPrimary = true;
                    dryKgPerDay += Get(p, "SolidsRemovedKgPerDay");
                    break;

                case UnitOperationType.Aeration:
                    dryKgPerDay += Math.Max(Get(p, "BodLoadKgPerDay")
                                            - basis.FlowM3PerDay * Get(p, "BodOutMgL") / 1000.0, 0) * bioYieldBod;
                    break;

                case UnitOperationType.Mbbr:
                    dryKgPerDay += Get(p, "CodRemovedKgPerDay") * bioYieldCodMbbr;
                    break;

                case UnitOperationType.AnaerobicReactor:
                    dryKgPerDay += Get(p, "CodRemovedKgPerDay") * bioYieldCodAnaerobic;
                    break;
            }
        }

        foreach (var item in list.Where(x => x.Type == UnitOperationType.CoagulationFlocculation))
        {
            var p = item.Parameters;

            dryKgPerDay += Get(p, "CoagulantKgPerDay") * chemicalSludgeFactor;

            if (!hasPrimary)
            {
                dryKgPerDay += basis.FlowM3PerDay * basis.TssMgL / 1000.0 * coagTssRemoval;
            }
        }

        if (feedSolidsFraction <= 0)
        {
            feedSolidsFraction = 0.008;
        }

        var sludgeM3PerDay = dryKgPerDay / (feedSolidsFraction * 1000.0);

        plan.InletFlowM3PerDay = Math.Round(basis.FlowM3PerDay, 2);
        plan.SludgeDryKgPerDay = Math.Round(dryKgPerDay, 2);
        plan.SludgeM3PerDay = Math.Round(sludgeM3PerDay, 2);
        plan.TreatedWaterM3PerDay = Math.Round(Math.Max(basis.FlowM3PerDay - sludgeM3PerDay, 0), 2);

        plan.AddNote($"Sludge balance: {plan.SludgeDryKgPerDay:0.0} kg dry solids/day at {feedSolidsFraction * 100:0.0}% = {plan.SludgeM3PerDay:0.0} m3/day thickener feed.");
    }

    private static double Get(IReadOnlyDictionary<string, double> parameters, string key) =>
        parameters.TryGetValue(key, out var value) ? value : 0;
}