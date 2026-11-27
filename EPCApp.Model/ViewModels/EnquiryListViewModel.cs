namespace EPCApp.Model.ViewModel;

public class EnquiryListItemViewModel
{
    public long Id { get; set; }

    public string EnquiryNo { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public int PlantType { get; set; }

    public string PlantTypeName { get; set; } = string.Empty;

    public string IndustryTypeName { get; set; } = string.Empty;

    public bool ZldRequired { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class EnquiryListViewModel
{
    public int? PlantType { get; set; }

    public int? Status { get; set; }

    public List<EnquiryListItemViewModel> Items { get; set; } = new();
}