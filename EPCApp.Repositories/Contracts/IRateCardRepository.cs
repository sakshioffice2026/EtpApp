using EPCApp.Database.Entities;

namespace EPCApp.Repositories.Contracts;

public interface IRateCardRepository : IGenericRepository<RateCard>
{
    Task<IReadOnlyList<RateCard>> GetActiveAsync(DateTime? asOf = null, CancellationToken cancellationToken = default);

    Task<RateCard?> GetByItemCodeAsync(string itemCode, DateTime? asOf = null, CancellationToken cancellationToken = default);
}