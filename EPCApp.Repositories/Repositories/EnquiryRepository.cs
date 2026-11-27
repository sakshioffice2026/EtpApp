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
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Enquiry?> GetWithStreamsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Streams)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Enquiry?> GetWithProposalsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Streams)
            .Include(x => x.Proposals)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Enquiry?> GetByEnquiryNoAsync(string enquiryNo, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EnquiryNo == enquiryNo, cancellationToken);

    public async Task<string> GenerateEnquiryNoAsync(PlantType plantType, CancellationToken cancellationToken = default)
    {
        var prefix = plantType switch
        {
            PlantType.Etp => "ENQ-ETP",
            PlantType.Stp => "ENQ-STP",
            PlantType.Zld => "ENQ-ZLD",
            _ => "ENQ"
        };

        var stem = $"{prefix}-{DateTime.UtcNow.Year}-";

        var existing = await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.EnquiryNo.StartsWith(stem))
            .Select(x => x.EnquiryNo)
            .ToListAsync(cancellationToken);

        var max = 0;

        foreach (var code in existing)
        {
            if (int.TryParse(code.AsSpan(stem.Length), out var number) && number > max)
                max = number;
        }

        return $"{stem}{max + 1:0000}";
    }

    public async Task ReplaceStreamsAsync(long enquiryId, IEnumerable<EnquiryStream> streams, CancellationToken cancellationToken = default)
    {
        var list = streams.ToList();

        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        await Context.Set<EnquiryStream>()
            .IgnoreQueryFilters()
            .Where(x => x.EnquiryId == enquiryId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var stream in list)
        {
            stream.Id = 0;
            stream.EnquiryId = enquiryId;
        }

        await Context.Set<EnquiryStream>().AddRangeAsync(list, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EnquiryStream?> GetStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default)
        => await Context.Set<EnquiryStream>()
            .FirstOrDefaultAsync(x => x.Id == streamId && x.EnquiryId == enquiryId, cancellationToken);

    public async Task UpdateStreamAsync(EnquiryStream stream, CancellationToken cancellationToken = default)
    {
        Context.Set<EnquiryStream>().Update(stream);
        await Context.SaveChangesAsync(cancellationToken);
    }
}