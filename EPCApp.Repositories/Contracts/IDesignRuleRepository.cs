using EPCApp.Database.Entities;
using EPCApp.Database.Enums;

namespace EPCApp.Repositories.Contracts;

public interface IDesignRuleRepository : IGenericRepository<DesignRule>
{
    Task<IReadOnlyList<DesignRule>> GetActiveByPlantAsync(PlantType plantType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DesignRule>> GetByCategoryAsync(PlantType plantType, string category, CancellationToken cancellationToken = default);

    Task<DesignRule?> GetByCodeAsync(PlantType plantType, string ruleCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DesignRule>> GetUnverifiedAsync(CancellationToken cancellationToken = default);
}