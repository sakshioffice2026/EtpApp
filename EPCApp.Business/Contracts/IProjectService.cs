using EPCApp.Model.DTOs;

namespace EPCApp.Business.Contracts;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(int? plantType = null, CancellationToken cancellationToken = default);

    Task<ProjectDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<string> GenerateProjectCodeAsync(int plantType, CancellationToken cancellationToken = default);

    Task<ProjectDto> CreateAsync(ProjectDto project, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ProjectDto project, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StreamInputDto>> GetStreamsAsync(long projectId, CancellationToken cancellationToken = default);

    Task<StreamInputDto> SaveStreamAsync(StreamInputDto stream, CancellationToken cancellationToken = default);

    Task<bool> DeleteStreamAsync(long streamId, CancellationToken cancellationToken = default);
}