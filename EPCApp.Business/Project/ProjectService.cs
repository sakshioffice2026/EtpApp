using EPCApp.Business.Contracts;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Model.DTOs;
using EPCApp.Repositories.Contracts;

namespace EPCApp.Business.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(int? plantType = null, CancellationToken cancellationToken = default)
    {
        var rows = await _projects.GetActiveAsync(plantType.HasValue ? (PlantType)plantType.Value : null, cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<ProjectDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _projects.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }


    public async Task<bool> UpdateAsync(ProjectDto project, CancellationToken cancellationToken = default)
    {
        var entity = await _projects.GetByIdAsync(project.Id, cancellationToken);
        if (entity is null) return false;

        entity.Name = project.Name.Trim();
        entity.ClientName = project.ClientName.Trim();
        entity.PlantType = (PlantType)project.PlantType;
        entity.IndustryType = (IndustryType)project.IndustryType;
        entity.ZldRequired = project.ZldRequired || project.PlantType == (int)PlantType.Zld;
        entity.SiteLocation = project.SiteLocation?.Trim();
        entity.Remarks = project.Remarks?.Trim();

        _projects.Update(entity);
        await _projects.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _projects.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        entity.IsDeleted = true;

        _projects.Update(entity);
        await _projects.SaveChangesAsync(cancellationToken);
        return true;
    }


    private static ProjectDto ToDto(Project x) => new()
    {
        Id = x.Id,
        EnquiryId = x.EnquiryId,
        SourceProposalId = x.SourceProposalId,
        ProjectCode = x.ProjectCode,
        Name = x.Name,
        ClientName = x.ClientName,
        PlantType = (int)x.PlantType,
        IndustryType = (int)x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Remarks = x.Remarks
    };

    
}