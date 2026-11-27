namespace EPCApp.Model.DTOs;

public class ProposalStatusHistoryDto
{
    public long Id { get; set; }

    public long ProposalId { get; set; }

    public int? FromStatus { get; set; }

    public string? FromStatusName { get; set; }

    public int ToStatus { get; set; }

    public string ToStatusName { get; set; } = string.Empty;

    public string? ChangedBy { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }
}
