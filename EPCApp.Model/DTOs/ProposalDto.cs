namespace EPCApp.Model.DTOs;

public class ProposalDto
{
    public long Id { get; set; }

    public long? ProjectId { get; set; }

    public long? EnquiryId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public int VersionNo { get; set; } = 1;

    public int Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }

    public List<string> TrainOperations { get; set; } = new();

    public List<EquipmentDto> Equipment { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();

    public List<ProposalLineItemDto> LineItems { get; set; } = new();

    public List<ProposalStatusHistoryDto> StatusHistory { get; set; } = new();
}
