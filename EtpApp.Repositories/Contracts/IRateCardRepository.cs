using EtpApp.Database.Entities;

namespace EtpApp.Repositories.Contracts;

public interface IRateCardRepository : IGenericRepository<RateCard>
{
    Task<IReadOnlyList<RateCard>> GetActiveAsync(DateTime? asOf = null, CancellationToken cancellationToken = default);

    Task<RateCard?> GetByItemCodeAsync(string itemCode, DateTime? asOf = null, CancellationToken cancellationToken = default);
}