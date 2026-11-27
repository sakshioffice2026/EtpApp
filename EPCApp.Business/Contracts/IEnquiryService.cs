using EPCApp.Model.DTOs;

namespace EPCApp.Business.Contracts;

public interface IEnquiryService
{
    Task<IReadOnlyList<EnquiryDto>> GetAllAsync(int? plantType = null, int? status = null, CancellationToken cancellationToken = default);

    Task<EnquiryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<string> GenerateEnquiryNoAsync(CancellationToken cancellationToken = default);

    Task<EnquiryDto> CreateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default);

    Task<bool> MarkLostAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StreamInputDto>> GetStreamsAsync(long enquiryId, CancellationToken cancellationToken = default);

    Task<StreamInputDto?> GetStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default);

    Task<StreamInputDto> SaveStreamAsync(StreamInputDto stream, CancellationToken cancellationToken = default);

    Task<bool> DeleteStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default);
}