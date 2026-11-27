using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class Proposal : BaseEntity
{
    public long EnquiryId { get; set; }

    public long? ProjectId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public int VersionNo { get; set; } = 1;

    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? Remarks { get; set; }

    public Enquiry Enquiry { get; set; } = null!;

    public Project? Project { get; set; }

    public ICollection<TreatmentTrain> Trains { get; set; } = new List<TreatmentTrain>();

    public ICollection<BoqLine> BoqLines { get; set; } = new List<BoqLine>();

    public ICollection<DrawingRecord> Drawings { get; set; } = new List<DrawingRecord>();
}