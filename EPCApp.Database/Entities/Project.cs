using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class Project : BaseEntity
{
    public string ProjectCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public PlantType PlantType { get; set; }

    public IndustryType IndustryType { get; set; }

    public bool ZldRequired { get; set; }

    public string? SiteLocation { get; set; }

    public string? Remarks { get; set; }

    public long? EnquiryId { get; set; }

    public long? SourceProposalId { get; set; }

    public bool IsDeleted { get; set; }

    public Enquiry? Enquiry { get; set; }

    public Proposal? SourceProposal { get; set; }

    public ICollection<WastewaterStream> Streams { get; set; } = new List<WastewaterStream>();

    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
}