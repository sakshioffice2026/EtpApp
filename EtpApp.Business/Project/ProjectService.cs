using EtpApp.Business.Contracts;
using EtpApp.Database.Entities;
using EtpApp.Database.Enums;
using EtpApp.Model.DTOs;
using EtpApp.Repositories.Contracts;

namespace EtpApp.Business.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;
    private readonly IGenericRepository<WastewaterStream> _streams;

    public ProjectService(IProjectRepository projects, IGenericRepository<WastewaterStream> streams)
    {
        _projects = projects;
        _streams = streams;
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

    public Task<string> GenerateProjectCodeAsync(int plantType, CancellationToken cancellationToken = default)
        => _projects.GenerateProjectCodeAsync((PlantType)plantType, cancellationToken);

    public async Task<ProjectDto> CreateAsync(ProjectDto project, CancellationToken cancellationToken = default)
    {
        var entity = new Project
        {
            ProjectCode = string.IsNullOrWhiteSpace(project.ProjectCode)
                ? await _projects.GenerateProjectCodeAsync((PlantType)project.PlantType, cancellationToken)
                : project.ProjectCode.Trim(),
            Name = project.Name.Trim(),
            ClientName = project.ClientName.Trim(),
            PlantType = (PlantType)project.PlantType,
            IndustryType = (IndustryType)project.IndustryType,
            ZldRequired = project.ZldRequired || project.PlantType == (int)PlantType.Zld,
            SiteLocation = project.SiteLocation?.Trim(),
            Remarks = project.Remarks?.Trim()
        };

        await _projects.AddAsync(entity, cancellationToken);
        await _projects.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
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

    public async Task<IReadOnlyList<StreamInputDto>> GetStreamsAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _streams.FindAsync(x => x.ProjectId == projectId && !x.IsDeleted, cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    public async Task<StreamInputDto> SaveStreamAsync(StreamInputDto stream, CancellationToken cancellationToken = default)
    {
        WastewaterStream? entity = null;

        if (stream.Id > 0)
            entity = await _streams.GetByIdAsync(stream.Id, cancellationToken);

        var isNew = entity is null;
        entity ??= new WastewaterStream { ProjectId = stream.ProjectId };

        entity.StreamName = stream.StreamName.Trim();
        entity.FlowM3PerDay = stream.FlowM3PerDay;
        entity.CodMgL = stream.CodMgL;
        entity.BodMgL = stream.BodMgL;
        entity.TssMgL = stream.TssMgL;
        entity.TdsMgL = stream.TdsMgL;
        entity.OilGreaseMgL = stream.OilGreaseMgL;
        entity.Ph = stream.Ph;
        entity.TemperatureC = stream.TemperatureC;

        if (isNew)
            await _streams.AddAsync(entity, cancellationToken);
        else
            _streams.Update(entity);

        await _streams.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<bool> DeleteStreamAsync(long streamId, CancellationToken cancellationToken = default)
    {
        var entity = await _streams.GetByIdAsync(streamId, cancellationToken);
        if (entity is null) return false;

        entity.IsDeleted = true;

        _streams.Update(entity);
        await _streams.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProjectDto ToDto(Project x) => new()
    {
        Id = x.Id,
        ProjectCode = x.ProjectCode,
        Name = x.Name,
        ClientName = x.ClientName,
        PlantType = (int)x.PlantType,
        IndustryType = (int)x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Remarks = x.Remarks
    };

    private static StreamInputDto ToDto(WastewaterStream x) => new()
    {
        Id = x.Id,
        ProjectId = x.ProjectId,
        StreamName = x.StreamName,
        FlowM3PerDay = x.FlowM3PerDay,
        CodMgL = x.CodMgL,
        BodMgL = x.BodMgL,
        TssMgL = x.TssMgL,
        TdsMgL = x.TdsMgL,
        OilGreaseMgL = x.OilGreaseMgL,
        Ph = x.Ph,
        TemperatureC = x.TemperatureC
    };
}