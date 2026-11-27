namespace EPCApp.Model.DTOs;

public class EquipmentDto
{
    public long Id { get; set; }

    public int SequenceNo { get; set; }

    public int UnitType { get; set; }

    public string UnitTypeName { get; set; } = string.Empty;

    public string TagNo { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Material { get; set; }

    public string MaterialName { get; set; } = string.Empty;

    public Dictionary<string, decimal> Parameters { get; set; } = new();
}