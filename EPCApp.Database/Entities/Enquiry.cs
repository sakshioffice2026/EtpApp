using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class Enquiry : BaseEntity
{
    public string EnquiryNo { get; set; } = string.Empty;

    public DateTime EnquiryDate { get; set; } = DateTime.UtcNow.Date;

    public string Title { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public PlantType PlantType { get; set; }

    public IndustryType IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public string? Remarks { get; set; }

    public EnquiryStatus Status { get; set; } = EnquiryStatus.New;

    public bool IsDeleted { get; set; }

    public ICollection<WastewaterStream> Streams { get; set; } = new List<WastewaterStream>();

    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();

    public Project? Project { get; set; }
}