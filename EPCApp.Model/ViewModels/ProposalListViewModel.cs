namespace EPCApp.Model.ViewModel;

public class ProposalListItemViewModel
{
    public long Id { get; set; }

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
}

public class ProposalListViewModel
{
    public int? Status { get; set; }

    public List<ProposalListItemViewModel> Items { get; set; } = new();
}
