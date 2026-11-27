using System.ComponentModel.DataAnnotations.Schema;
using EtpApp.Database.Entities.Common;

namespace EtpApp.Database.Entities;

public class BoqLine : BaseEntity
{
    public long ProposalId { get; set; }

    public long? EquipmentItemId { get; set; }

    public string? TagNo { get; set; }

    public string ItemDescription { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal Amount { get; private set; }

    public Proposal Proposal { get; set; } = null!;

    public EquipmentItem? EquipmentItem { get; set; }
}