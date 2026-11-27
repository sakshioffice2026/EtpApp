using EPCApp.Model.DTOs;

namespace EPCApp.Business.Contracts;

public interface IEnquiryService
{
    Task<IReadOnlyList<EnquiryDto>> GetAllAsync(int? plantType = null, int? status = null, CancellationToken cancellationToken = default);

    Task<EnquiryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<string> GenerateEnquiryNoAsync(int plantType, CancellationToken cancellationToken = default);

    Task<EnquiryDto> CreateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default);


    Task<EnquiryStreamDto?> GetStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default);

    Task<bool> UpdateStreamAsync(long enquiryId, EnquiryStreamDto stream, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> SetStatusAsync(long id, int status, CancellationToken cancellationToken = default);
}
