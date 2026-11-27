
using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class DrawingRecord : BaseEntity
{
    public long ProposalId { get; set; }

    public DrawingType DrawingType { get; set; }

    public string? DrawingNo { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public Proposal Proposal { get; set; } = null!;
}