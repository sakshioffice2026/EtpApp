using EPCApp.Database.Enums;

namespace EPCApp.Database.Entities;

public class Proposal : BaseEntity
{
    public long ProjectId { get; set; }

    public string ProposalNo { get; set; } = string.Empty;

    public int VersionNo { get; set; } = 1;

    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;

    public string CurrencyCode { get; set; } = "INR";

    public decimal TotalCost { get; set; }

    public Project Project { get; set; } = null!;

    public ICollection<TreatmentTrain> Trains { get; set; } = new List<TreatmentTrain>();

    public ICollection<BoqLine> BoqLines { get; set; } = new List<BoqLine>();

    public ICollection<DrawingRecord> Drawings { get; set; } = new List<DrawingRecord>();
}