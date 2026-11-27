using EPCApp.Model.DTOs;

namespace EPCApp.Model.ViewModel;

public class ProposalDetailViewModel
{
    public long ProposalId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public int VersionNo { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public long? EnquiryId { get; set; }

    public string? EnquiryNo { get; set; }

    public long? ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string PlantTypeName { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool CanRecalculate { get; set; }

    public bool CanSend { get; set; }

    public bool CanAccept { get; set; }

    public bool CanReject { get; set; }

    public bool CanRevise { get; set; }

    public List<string> TrainOperations { get; set; } = new();

    public List<EquipmentDto> Equipment { get; set; } = new();

    public List<BoqLineDto> BoqLines { get; set; } = new();

    public List<ProposalLineItemDto> LineItems { get; set; } = new();

    public List<ProposalStatusHistoryDto> StatusHistory { get; set; } = new();

    public List<EnquiryStreamDto> Streams { get; set; } = new();
}
