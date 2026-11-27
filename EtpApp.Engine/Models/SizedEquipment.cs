using EtpApp.Database.Enums;

namespace EtpApp.Engine.Models;

public sealed record SizedEquipment(
    string Tag,
    UnitOperationType Type,
    string Description,
    MaterialOfConstruction Material,
    IReadOnlyDictionary<string, double> Parameters);