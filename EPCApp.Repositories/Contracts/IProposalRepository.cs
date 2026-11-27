using EPCApp.Database.Entities;

namespace EPCApp.Repositories.Contracts;

public interface IProposalRepository : IGenericRepository<Proposal>
{
    Task<IReadOnlyList<Proposal>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Proposal>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<Proposal?> GetFullAsync(long id, CancellationToken cancellationToken = default);

    Task<Proposal?> GetLatestByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNoAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<string> GenerateProposalNoAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        CancellationToken cancellationToken = default);
}