

namespace EtpApp.Database.Entities;


public class TreatmentTrain : BaseEntity
{
    public long ProposalId { get; set; }

    public string TrainName { get; set; } = string.Empty;

    public bool IsUserOverride { get; set; }

    public Proposal Proposal { get; set; } = null!;

    public ICollection<UnitOperation> UnitOperations { get; set; } = new List<UnitOperation>();
}