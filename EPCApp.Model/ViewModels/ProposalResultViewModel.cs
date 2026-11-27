using EPCApp.Model.DTOs;

namespace EPCApp.Model.ViewModel;

public class ProposalResultViewModel
{
    public long ProposalId { get; set; }


    public long EnquiryId { get; set; }

    public long? ProjectId { get; set; }

    public string EnquiryNo { get; set; } = string.Empty;

    public int VersionNo { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public string ProposalNo { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string PlantTypeName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? Remarks { get; set; }
    public List<ProposalLineItemDto> LineItems { get; set; } = new();

    public List<string> TrainOperations { get; set; } = new();

    public List<EquipmentDto> Equipment { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();
}