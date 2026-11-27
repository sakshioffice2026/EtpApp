namespace EPCApp.Engine.Costing;

public sealed class RateSheet
{
    public double ExcavationPerM3 { get; init; } = 350;

    public double PccPerM3 { get; init; } = 5500;

    public double RccPerM3 { get; init; } = 9500;

    public double ShutteringPerM2 { get; init; } = 650;

    public double ReinforcementPerKg { get; init; } = 75;

    public double WaterproofingPerM2 { get; init; } = 450;

    public double ReinforcementKgPerM3 { get; init; } = 110;

    public static RateSheet FromItemCodes(IReadOnlyDictionary<string, double> rates)
    {
        var defaults = new RateSheet();

        double Get(string code, double fallback) =>
            rates.TryGetValue(code, out var value) ? value : fallback;

        return new RateSheet
        {
            ExcavationPerM3 = Get("EXCAVATION", defaults.ExcavationPerM3),
            PccPerM3 = Get("PCC", defaults.PccPerM3),
            RccPerM3 = Get("RCC_M30", defaults.RccPerM3),
            ShutteringPerM2 = Get("SHUTTERING", defaults.ShutteringPerM2),
            ReinforcementPerKg = Get("REINFORCEMENT", defaults.ReinforcementPerKg),
            WaterproofingPerM2 = Get("WATERPROOFING", defaults.WaterproofingPerM2)
        };
    }
}