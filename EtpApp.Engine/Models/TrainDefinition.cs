using EtpApp.Database.Enums;

namespace EtpApp.Engine.Models;

public sealed class TrainDefinition
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<UnitOperationType> Operations { get; init; } = Array.Empty<UnitOperationType>();
}