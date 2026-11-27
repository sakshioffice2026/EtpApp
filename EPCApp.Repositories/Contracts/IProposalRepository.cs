using EPCApp.Database.Entities;
using EPCApp.Database.Enums;

namespace EPCApp.Repositories.Contracts;

public interface IProposalRepository : IGenericRepository<Proposal>
{
    Task<IReadOnlyList<Proposal>> GetListAsync(ProposalStatus? status = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Proposal>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Proposal>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<Proposal?> GetFullAsync(long id, CancellationToken cancellationToken = default);

    Task<Proposal?> GetLatestByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<Proposal?> GetLatestByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNoAsync(long projectId, CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNoByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<string> GenerateProposalNoAsync(long projectId, CancellationToken cancellationToken = default);

    Task<string> GenerateProposalNoForEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        CancellationToken cancellationToken = default);

    Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        IEnumerable<ProposalLineItem> lineItems,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        long proposalId,
        ProposalStatus newStatus,
        string? changedBy = null,
        string? remarks = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalStatusHistory>> GetHistoryAsync(long proposalId, CancellationToken cancellationToken = default);
}