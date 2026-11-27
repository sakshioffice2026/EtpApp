using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Zld;

public sealed record ZldHandoffResult(
    double RoFeedM3PerDay,
    double PermeateM3PerDay,
    double RejectM3PerDay,
    double PermeateTdsMgL,
    double RejectTdsMgL);

public static class ZldHandoff
{
    public static ZldHandoffResult Compute(StreamBasis basis, DesignBasis design, TrainPlan plan)
    {
        var recovery = design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_RECOVERY", "RecoveryFraction"), 0.75);
        var rejection = design.Rule(DesignBasis.RuleKey("ReverseOsmosis", "RO_SALT_REJECTION", "RejectionFraction"), 0.98);
        var ufRecovery = design.Rule(DesignBasis.RuleKey("UltraFiltration", "UF_RECOVERY", "RecoveryFraction"), 0.92);

        recovery = Math.Clamp(recovery, 0.10, 0.95);
        rejection = Math.Clamp(rejection, 0.50, 0.999);
        ufRecovery = Math.Clamp(ufRecovery, 0.50, 1.0);

        var treatedM3PerDay = plan.TreatedWaterM3PerDay > 0 ? plan.TreatedWaterM3PerDay : basis.FlowM3PerDay;
        var roFeedM3PerDay = treatedM3PerDay * ufRecovery;
        var permeateM3PerDay = roFeedM3PerDay * recovery;
        var rejectM3PerDay = roFeedM3PerDay - permeateM3PerDay;

        var feedTdsMgL = basis.TdsMgL;
        var permeateTdsMgL = feedTdsMgL * (1.0 - rejection);
        var rejectTdsMgL = rejectM3PerDay > 0
            ? (feedTdsMgL * roFeedM3PerDay - permeateTdsMgL * permeateM3PerDay) / rejectM3PerDay
            : feedTdsMgL;

        plan.RejectM3PerDay = Math.Round(rejectM3PerDay, 2);
        plan.ZldFeedM3PerDay = basis.ZldRequired ? plan.RejectM3PerDay : 0;
        plan.TreatedWaterM3PerDay = Math.Round(permeateM3PerDay, 2);

        plan.AddNote($"RO: feed {roFeedM3PerDay:0.0} m3/day at {recovery * 100:0}% recovery gives {permeateM3PerDay:0.0} m3/day permeate and {rejectM3PerDay:0.0} m3/day reject at {rejectTdsMgL:0} mg/L TDS.");

        if (basis.ZldRequired)
        {
            plan.AddNote($"ZLD hand-off: {plan.ZldFeedM3PerDay:0.0} m3/day RO reject to softener, evaporator and ATFD.");
        }

        return new ZldHandoffResult(
            Math.Round(roFeedM3PerDay, 2),
            Math.Round(permeateM3PerDay, 2),
            Math.Round(rejectM3PerDay, 2),
            Math.Round(permeateTdsMgL, 1),
            Math.Round(rejectTdsMgL, 1));
    }
}