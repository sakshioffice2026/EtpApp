
using EtpApp.Database.Enums;

namespace EtpApp.Database.Entities;

public class UnitOperation : BaseEntity
{
    public long TrainId { get; set; }

    public int SequenceNo { get; set; }

    public UnitOperationType UnitType { get; set; }

    public TreatmentTrain Train { get; set; } = null!;

    public ICollection<EquipmentItem> EquipmentItems { get; set; } = new List<EquipmentItem>();
}