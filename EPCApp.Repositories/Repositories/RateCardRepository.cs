using EPCApp.Database.Context;
using EPCApp.Database.Entities;
using EPCApp.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EPCApp.Repositories.Repositories;

public class RateCardRepository : GenericRepository<RateCard>, IRateCardRepository
{
    public RateCardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<RateCard>> GetActiveAsync(DateTime? asOf = null, CancellationToken cancellationToken = default)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;

        var rows = await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= date)
            .OrderBy(x => x.ItemCode)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.ItemCode)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<RateCard?> GetByItemCodeAsync(string itemCode, DateTime? asOf = null, CancellationToken cancellationToken = default)
    {
        var date = (asOf ?? DateTime.UtcNow).Date;

        return await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive && x.ItemCode == itemCode && x.EffectiveFrom <= date)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }
}