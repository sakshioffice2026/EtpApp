using EtpApp.Model.DTOs;
using EtpApp.Model.ViewModel;

namespace EtpApp.Business.Contracts;

public interface IProposalService
{
    Task<ProposalDto> CalculateAsync(long projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<ProposalDto?> GetByIdAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalResultViewModel?> GetResultAsync(long proposalId, CancellationToken cancellationToken = default);
}