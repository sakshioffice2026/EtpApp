using EPCApp.Database.Entities;
using EPCApp.Database.Enums;

namespace EPCApp.Repositories.Contracts;

public interface IEnquiryRepository : IGenericRepository<Enquiry>
{
    Task<IReadOnlyList<Enquiry>> GetActiveAsync(PlantType? plantType = null, EnquiryStatus? status = null, CancellationToken cancellationToken = default);

    Task<Enquiry?> GetWithStreamsAsync(long id, CancellationToken cancellationToken = default);

    Task<Enquiry?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<string> GenerateEnquiryNoAsync(CancellationToken cancellationToken = default);
}