using EtpApp.Database.Entities;

namespace EtpApp.Repositories.Contracts;

public interface IProposalRepository : IGenericRepository<Proposal>
{
    Task<IReadOnlyList<Proposal>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<Proposal?> GetFullAsync(long id, CancellationToken cancellationToken = default);

    Task<Proposal?> GetLatestByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<int> GetNextVersionNoAsync(long projectId, CancellationToken cancellationToken = default);

    Task<string> GenerateProposalNoAsync(long projectId, CancellationToken cancellationToken = default);

    Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        CancellationToken cancellationToken = default);
}