using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;

namespace EPCApp.Business.Contracts;

public interface IProposalService
{
    Task<ProposalDto> CalculateAsync(long projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProposalDto>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<ProposalDto?> GetByIdAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalResultViewModel?> GetResultAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalListViewModel> GetListAsync(int? status = null, CancellationToken cancellationToken = default);

    Task<ProposalDetailViewModel?> GetDetailAsync(long proposalId, CancellationToken cancellationToken = default);

    Task<ProposalDto> CreateFromEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<ProposalDto> RecalculateAsync(long proposalId, CancellationToken cancellationToken = default);

    Task SendAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default);

    Task<long> AcceptAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default);

    Task RejectAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default);

    Task<ProposalDto> ReviseAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default);
}
