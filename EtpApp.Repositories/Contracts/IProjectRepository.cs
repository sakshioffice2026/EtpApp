using EtpApp.Database.Entities;
using EtpApp.Database.Enums;

namespace EtpApp.Repositories.Contracts;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<IReadOnlyList<Project>> GetActiveAsync(PlantType? plantType = null, CancellationToken cancellationToken = default);

    Task<Project?> GetWithStreamsAsync(long id, CancellationToken cancellationToken = default);

    Task<Project?> GetWithProposalsAsync(long id, CancellationToken cancellationToken = default);

    Task<Project?> GetByCodeAsync(string projectCode, CancellationToken cancellationToken = default);

    Task<string> GenerateProjectCodeAsync(PlantType plantType, CancellationToken cancellationToken = default);
}