using EPCApp.Database.Context;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EPCApp.Repositories.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Project>> GetActiveAsync(PlantType? plantType = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted);

        if (plantType.HasValue && plantType.Value != PlantType.All)
            query = query.Where(x => x.PlantType == plantType.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project?> GetWithStreamsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Streams)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Project?> GetWithProposalsAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Proposals)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectCode == projectCode, cancellationToken);

    public async Task<string> GenerateProjectCodeAsync(PlantType plantType, CancellationToken cancellationToken = default)
    {
        var prefix = plantType switch
        {
            PlantType.Etp => "ETP",
            PlantType.Stp => "STP",
            PlantType.Zld => "ZLD",
            _ => "PRJ"
        };

        var stem = $"{prefix}-{DateTime.UtcNow.Year}-";

        var existing = await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.ProjectCode.StartsWith(stem))
            .Select(x => x.ProjectCode)
            .ToListAsync(cancellationToken);

        var max = 0;

        foreach (var code in existing)
        {
            if (int.TryParse(code.AsSpan(stem.Length), out var number) && number > max)
                max = number;
        }

        return $"{stem}{max + 1:0000}";
    }
}