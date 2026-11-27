using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;

namespace EPCApp.Business.Contracts;

public interface IProposalService
{
    Task<ProposalDto> CalculateAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<bool> SendAsync(long proposalId, DateTime? validUntil = null, CancellationToken cancellationToken = default);

    Task<long> AcceptAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<bool> RejectAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<ProposalDto?> GetByIdAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalResultViewModel?> GetResultAsync(long proposalId, CancellationToken cancellationToken = default);
}