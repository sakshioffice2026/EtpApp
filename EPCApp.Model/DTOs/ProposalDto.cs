namespace EPCApp.Model.DTOs;

public class ProposalDto
{
    public long Id { get; set; }

    public long EnquiryId { get; set; }

    public long? ProjectId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public int VersionNo { get; set; } = 1;

    public int Status { get; set; }

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }


    public DateTime? ValidUntil { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? Remarks { get; set; }

    public List<string> TrainOperations { get; set; } = new();

    public List<EquipmentDto> Equipment { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();
}