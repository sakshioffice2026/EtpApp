namespace EPCApp.Engine.Models;

public sealed record BoqLineItem(
    string Tag,
    string Item,
    string Unit,
    double Quantity,
    double Rate)
{
    public double Amount => Math.Round(Quantity * Rate, 2);
}