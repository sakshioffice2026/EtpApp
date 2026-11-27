using EtpApp.Database.Context;
using EtpApp.Database.Entities;
using EtpApp.Database.Enums;
using EtpApp.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EtpApp.Repositories.Repositories;

public class DesignRuleRepository : GenericRepository<DesignRule>, IDesignRuleRepository
{
    public DesignRuleRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DesignRule>> GetActiveByPlantAsync(PlantType plantType, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive && (x.PlantType == plantType || x.PlantType == PlantType.All))
            .OrderBy(x => x.Category)
            .ThenBy(x => x.RuleCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DesignRule>> GetByCategoryAsync(PlantType plantType, string category, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => x.IsActive
                        && x.Category == category
                        && (x.PlantType == plantType || x.PlantType == PlantType.All))
            .OrderBy(x => x.RuleCode)
            .ToListAsync(cancellationToken);

    public async Task<DesignRule?> GetByCodeAsync(PlantType plantType, string ruleCode, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive
                                      && x.RuleCode == ruleCode
                                      && (x.PlantType == plantType || x.PlantType == PlantType.All),
                cancellationToken);

    public async Task<IReadOnlyList<DesignRule>> GetUnverifiedAsync(CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => !x.IsVerified)
            .OrderBy(x => x.PlantType)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.RuleCode)
            .ToListAsync(cancellationToken);
}