using EPCApp.Database.Enums;

public sealed class TrainDefinition
{
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<UnitOperationType> Operations { get; init; } = Array.Empty<UnitOperationType>();

    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();

    public bool IncludesZld { get; init; }
}