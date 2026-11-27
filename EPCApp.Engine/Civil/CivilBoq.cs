using EPCApp.Engine.Costing;
using EPCApp.Engine.Models;

namespace EPCApp.Engine.Civil;

public static class CivilBoq
{
    private const double WallThicknessM = 0.30;
    private const double BaseSlabThicknessM = 0.35;
    private const double PccThicknessM = 0.10;
    private const double ExcavationMarginM = 1.0;

    public static IReadOnlyList<BoqLineItem> Rectangular(
        string tag, double lengthM, double widthM, double depthM, RateSheet rates)
    {
        var outerLength = lengthM + 2 * WallThicknessM;
        var outerWidth = widthM + 2 * WallThicknessM;

        var excavation = (outerLength + ExcavationMarginM) * (outerWidth + ExcavationMarginM)
                         * (depthM + BaseSlabThicknessM + PccThicknessM);
        var pcc = outerLength * outerWidth * PccThicknessM;
        var baseSlab = outerLength * outerWidth * BaseSlabThicknessM;
        var walls = 2 * ((lengthM + WallThicknessM) + (widthM + WallThicknessM)) * WallThicknessM * depthM;
        var shuttering = 4 * (lengthM + widthM) * depthM;
        var waterproofing = 2 * (lengthM + widthM) * depthM + lengthM * widthM;

        return Compose(tag, excavation, pcc, baseSlab + walls, shuttering, waterproofing, rates);
    }

    public static IReadOnlyList<BoqLineItem> Circular(
        string tag, double diameterM, double depthM, RateSheet rates)
    {
        var outerDiameter = diameterM + 2 * WallThicknessM;
        var outerArea = Math.PI / 4.0 * outerDiameter * outerDiameter;

        var excavation = Math.PI / 4.0 * Math.Pow(outerDiameter + ExcavationMarginM, 2)
                         * (depthM + BaseSlabThicknessM + PccThicknessM);
        var pcc = outerArea * PccThicknessM;
        var baseSlab = outerArea * BaseSlabThicknessM;
        var walls = Math.PI * (diameterM + WallThicknessM) * WallThicknessM * depthM;
        var shuttering = Math.PI * (diameterM + outerDiameter) * depthM;
        var waterproofing = Math.PI * diameterM * depthM + Math.PI / 4.0 * diameterM * diameterM;

        return Compose(tag, excavation, pcc, baseSlab + walls, shuttering, waterproofing, rates);
    }

    private static IReadOnlyList<BoqLineItem> Compose(
        string tag, double excavation, double pcc, double rcc,
        double shuttering, double waterproofing, RateSheet rates)
    {
        var reinforcement = rcc * rates.ReinforcementKgPerM3;

        return new List<BoqLineItem>
        {
            new(tag, "Excavation in ordinary soil", "m3", Math.Round(excavation, 2), rates.ExcavationPerM3),
            new(tag, "Plain cement concrete", "m3", Math.Round(pcc, 2), rates.PccPerM3),
            new(tag, "Reinforced cement concrete", "m3", Math.Round(rcc, 2), rates.RccPerM3),
            new(tag, "Shuttering and centering", "m2", Math.Round(shuttering, 2), rates.ShutteringPerM2),
            new(tag, "Reinforcement steel", "kg", Math.Round(reinforcement, 2), rates.ReinforcementPerKg),
            new(tag, "Waterproofing treatment", "m2", Math.Round(waterproofing, 2), rates.WaterproofingPerM2)
        };
    }
}