using EPCApp.Model.DTOs;

namespace EPCApp.Business.Contracts;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(int? plantType = null, CancellationToken cancellationToken = default);

    Task<ProjectDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProjectDto project, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}