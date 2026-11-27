using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class Enquiry : BaseEntity
{
    public string EnquiryNo { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public PlantType PlantType { get; set; }

    public IndustryType IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public EnquiryStatus Status { get; set; } = EnquiryStatus.Draft;

    public string? Remarks { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<EnquiryStream> Streams { get; set; } = new List<EnquiryStream>();

    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
}