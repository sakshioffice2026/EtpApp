using System.ComponentModel.DataAnnotations.Schema;
using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class ProposalLineItem : BaseEntity
{
    public long ProposalId { get; set; }

    public BoqCategory Category { get; set; }

    public int SortOrder { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Rate { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal Amount { get; private set; }

    public Proposal Proposal { get; set; } = null!;
}