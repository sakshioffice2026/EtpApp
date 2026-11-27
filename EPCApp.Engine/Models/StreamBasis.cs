
using EPCApp.Database.Enums;

namespace EPCApp.Engine.Models;

public sealed record StreamBasis(
    double FlowM3PerDay,
    double CodMgL,
    double BodMgL,
    double TssMgL,
    double TdsMgL,
    double OilGreaseMgL,
    double Ph,
    double TemperatureC,
    IndustryType Industry,
    bool ZldRequired)
{
    public double BodCodRatio => CodMgL > 0 ? BodMgL / CodMgL : 0;
}