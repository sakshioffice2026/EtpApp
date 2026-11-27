using EPCApp.Database.Enums;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Sizing.Sludge;

public static class FilterPressSizer
{
    public static SizedEquipment Size(string tag, TrainPlan plan, DesignBasis design) =>
        Size(
            tag,
            plan,
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_CAKE_SOLIDS", "CakeSolidsFraction"), 0.30),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_CYCLE", "CycleHours"), 4),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_CYCLES_PER_DAY", "MaxCyclesPerDay"), 4),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_CAKE_DENSITY", "CakeDensityKgPerM3"), 1100),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_CHAMBER_THICKNESS", "ChamberThicknessM"), 0.032),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_PLATE_SIZE", "PlateSizeM"), 1.0),
            design.Rule(DesignBasis.RuleKey("FilterPress", "FP_FEED_PRESSURE", "FeedPressureBarG"), 7));

    public static SizedEquipment Size(
        string tag,
        TrainPlan plan,
        double cakeSolidsFraction = 0.30,
        double cycleHours = 4,
        double maxCyclesPerDay = 4,
        double cakeDensityKgPerM3 = 1100,
        double chamberThicknessM = 0.032,
        double plateSizeM = 1.0,
        double feedPressureBarG = 7)
    {
        if (plan.SludgeDryKgPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(plan), "Sludge dry solids must be computed first.");
        if (cakeSolidsFraction <= 0 || cakeSolidsFraction >= 1) throw new ArgumentOutOfRangeException(nameof(cakeSolidsFraction));
        if (maxCyclesPerDay <= 0) throw new ArgumentOutOfRangeException(nameof(maxCyclesPerDay));

        var dryKgPerDay = plan.SludgeDryKgPerDay;
        var feedM3PerDay = plan.SludgeM3PerDay;

        var cakeKgPerDay = dryKgPerDay / cakeSolidsFraction;
        var cakeM3PerDay = cakeKgPerDay / cakeDensityKgPerM3;
        var cakeM3PerCycle = cakeM3PerDay / maxCyclesPerDay;

        var chamberVolumeM3 = plateSizeM * plateSizeM * chamberThicknessM * 0.85;
        var chambers = Math.Max(1, (int)Math.Ceiling(cakeM3PerCycle / chamberVolumeM3));
        var filtrateM3PerDay = Math.Max(feedM3PerDay - cakeKgPerDay / 1000.0, 0);

        var parameters = new Dictionary<string, double>
        {
            ["FeedM3PerDay"] = Math.Round(feedM3PerDay, 2),
            ["FeedDryKgPerDay"] = Math.Round(dryKgPerDay, 2),
            ["CakeSolidsFraction"] = cakeSolidsFraction,
            ["CakeKgPerDay"] = Math.Round(cakeKgPerDay, 1),
            ["CakeM3PerDay"] = Math.Round(cakeM3PerDay, 3),
            ["CyclesPerDay"] = maxCyclesPerDay,
            ["CycleHours"] = cycleHours,
            ["CakeM3PerCycle"] = Math.Round(cakeM3PerCycle, 3),
            ["PlateSizeM"] = plateSizeM,
            ["ChamberThicknessM"] = chamberThicknessM,
            ["Chambers"] = chambers,
            ["Plates"] = chambers + 1,
            ["FeedPressureBarG"] = feedPressureBarG,
            ["FiltrateM3PerDay"] = Math.Round(filtrateM3PerDay, 2)
        };

        plan.CakeKgPerDay = Math.Round(cakeKgPerDay, 1);
        plan.TreatedWaterM3PerDay = Math.Round(Math.Max(plan.InletFlowM3PerDay - (cakeKgPerDay - dryKgPerDay) / 1000.0, 0), 2);
        plan.AddNote($"Filter press: {plan.CakeKgPerDay:0} kg/day cake at {cakeSolidsFraction * 100:0}% solids; {filtrateM3PerDay:0.0} m3/day filtrate returns to equalization.");

        return new SizedEquipment(
            tag,
            UnitOperationType.FilterPress,
            $"Filter press {plateSizeM * 1000:0} mm x {plateSizeM * 1000:0} mm, {chambers} chambers, {cycleHours:0.#} h cycle",
            MaterialOfConstruction.MildSteel,
            parameters);
    }
}