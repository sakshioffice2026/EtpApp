using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;

namespace EPCApp.Business.Contracts;

public interface IProposalService
{
    Task<ProposalDto> CalculateAsync(long projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<ProposalDto?> GetByIdAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalResultViewModel?> GetResultAsync(long proposalId, CancellationToken cancellationToken = default);
}