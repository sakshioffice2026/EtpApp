using EtpApp.Database.Entities.Common;
using EtpApp.Database.Enums;


namespace EtpApp.Database.Entities;

public class EquipmentItem : BaseEntity
{
    public long UnitOperationId { get; set; }

    public string TagNo { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public MaterialOfConstruction Material { get; set; }

    public string? ParametersJson { get; set; }

    public UnitOperation UnitOperation { get; set; } = null!;

    public ICollection<BoqLine> BoqLines { get; set; } = new List<BoqLine>();
}