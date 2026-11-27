using EPCApp.Database.Enums;

namespace EPCApp.Engine.Models;

public sealed record BoqLineItem(
    string Tag,
    string Item,
    string Unit,
    double Quantity,
        double Rate,
    BoqCategory Category = BoqCategory.Civil)
{
    public double Amount => Math.Round(Quantity * Rate, 2);
}