using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class ProposalStatusHistory : BaseEntity
{
    public long ProposalId { get; set; }

    public ProposalStatus? FromStatus { get; set; }

    public ProposalStatus ToStatus { get; set; }

    public string? ChangedBy { get; set; }

    public string? Remarks { get; set; }

    public Proposal Proposal { get; set; } = null!;
}