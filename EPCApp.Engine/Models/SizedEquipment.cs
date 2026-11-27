using EPCApp.Database.Enums;

namespace EPCApp.Engine.Models;

public sealed record SizedEquipment(
    string Tag,
    UnitOperationType Type,
    string Description,
    MaterialOfConstruction Material,
    IReadOnlyDictionary<string, double> Parameters);