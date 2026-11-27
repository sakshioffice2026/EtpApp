using EPCApp.Database.Enums;

namespace EPCApp.Engine.Models;

public sealed class TrainDefinition
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<UnitOperationType> Operations { get; init; } = Array.Empty<UnitOperationType>();
}