using EPCApp.Database.Context;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EPCApp.Repositories.Repositories;

public class EnquiryRepository : GenericRepository<Enquiry>, IEnquiryRepository
{
    public EnquiryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Enquiry>> GetActiveAsync(PlantType? plantType = null, EnquiryStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted);

        if (plantType.HasValue && plantType.Value != PlantType.All)
            query = query.Where(x => x.PlantType == plantType.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.EnquiryDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Enquiry?> GetWithStreamsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Streams)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Enquiry?> GetWithDetailsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Streams)
            .Include(x => x.Proposals)
            .Include(x => x.Project)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<string> GenerateEnquiryNoAsync(CancellationToken cancellationToken = default)
    {
        var stem = $"ENQ-{DateTime.UtcNow.Year}-";

        var existing = await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.EnquiryNo.StartsWith(stem))
            .Select(x => x.EnquiryNo)
            .ToListAsync(cancellationToken);

        var max = 0;

        foreach (var enquiryNo in existing)
        {
            if (int.TryParse(enquiryNo.AsSpan(stem.Length), out var number) && number > max)
                max = number;
        }

        return $"{stem}{max + 1:0000}";
    }
}