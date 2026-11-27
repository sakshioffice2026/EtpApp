using EPCApp.Business.Contracts;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Model.DTOs;
using EPCApp.Repositories.Contracts;

namespace EPCApp.Business.Services;

public sealed class EnquiryService : IEnquiryService
{
    private readonly IEnquiryRepository _enquiries;
    private readonly IGenericRepository<WastewaterStream> _streams;

    public EnquiryService(IEnquiryRepository enquiries, IGenericRepository<WastewaterStream> streams)
    {
        _enquiries = enquiries;
        _streams = streams;
    }

    public async Task<IReadOnlyList<EnquiryDto>> GetAllAsync(int? plantType = null, int? status = null, CancellationToken cancellationToken = default)
    {
        var rows = await _enquiries.GetActiveAsync(
            plantType.HasValue ? (PlantType)plantType.Value : null,
            status.HasValue ? (EnquiryStatus)status.Value : null,
            cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<EnquiryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetWithDetailsAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public Task<string> GenerateEnquiryNoAsync(CancellationToken cancellationToken = default)
        => _enquiries.GenerateEnquiryNoAsync(cancellationToken);

    public async Task<EnquiryDto> CreateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default)
    {
        var entity = new Enquiry
        {
            EnquiryNo = await _enquiries.GenerateEnquiryNoAsync(cancellationToken),
            Status = EnquiryStatus.New
        };

        Apply(enquiry, entity);

        await _enquiries.AddAsync(entity, cancellationToken);
        await _enquiries.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<bool> UpdateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(enquiry.Id, cancellationToken);
        if (entity is null || entity.Status == EnquiryStatus.Won) return false;

        Apply(enquiry, entity);

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkLostAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.Status == EnquiryStatus.Won) return false;

        entity.Status = EnquiryStatus.Lost;

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (entity is null || entity.Status == EnquiryStatus.Won) return false;

        entity.IsDeleted = true;

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StreamInputDto>> GetStreamsAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var rows = await _streams.FindAsync(x => x.EnquiryId == enquiryId && !x.IsDeleted, cancellationToken);
        return rows.OrderBy(x => x.Id).Select(ToDto).ToList();
    }

    public async Task<StreamInputDto?> GetStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default)
    {
        var entity = await _streams.GetByIdAsync(streamId, cancellationToken);
        return entity is null || entity.EnquiryId != enquiryId ? null : ToDto(entity);
    }

    public async Task<StreamInputDto> SaveStreamAsync(StreamInputDto stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stream.StreamName))
            throw new InvalidOperationException("Stream name is required.");

        if (stream.FlowM3PerDay <= 0)
            throw new InvalidOperationException("Flow must be greater than zero.");

        var enquiry = await _enquiries.GetWithDetailsAsync(stream.EnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {stream.EnquiryId} not found.");

        var isNew = stream.Id <= 0;
        WastewaterStream entity;

        if (isNew)
        {
            entity = new WastewaterStream { EnquiryId = enquiry.Id };
        }
        else
        {
            var existing = await _streams.GetByIdAsync(stream.Id, cancellationToken);
            if (existing is null || existing.EnquiryId != enquiry.Id)
                throw new InvalidOperationException($"Stream {stream.Id} not found.");

            entity = existing;
        }

        entity.ProjectId = enquiry.Project?.Id;
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

    public async Task<bool> DeleteStreamAsync(long enquiryId, long streamId, CancellationToken cancellationToken = default)
    {
        var entity = await _streams.GetByIdAsync(streamId, cancellationToken);
        if (entity is null || entity.EnquiryId != enquiryId) return false;

        entity.IsDeleted = true;

        _streams.Update(entity);
        await _streams.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(EnquiryDto source, Enquiry target)
    {
        target.EnquiryDate = source.EnquiryDate.Date;
        target.Title = source.Title.Trim();
        target.ClientName = source.ClientName.Trim();
        target.ContactPerson = source.ContactPerson?.Trim();
        target.ContactEmail = source.ContactEmail?.Trim();
        target.ContactPhone = source.ContactPhone?.Trim();
        target.PlantType = (PlantType)source.PlantType;
        target.IndustryType = (IndustryType)source.IndustryType;
        target.ZldRequired = source.ZldRequired || source.PlantType == (int)PlantType.Zld;
        target.SiteLocation = source.SiteLocation?.Trim();
        target.Remarks = source.Remarks?.Trim();
    }

    private static EnquiryDto ToDto(Enquiry x) => new()
    {
        Id = x.Id,
        EnquiryNo = x.EnquiryNo,
        EnquiryDate = x.EnquiryDate,
        Title = x.Title,
        ClientName = x.ClientName,
        ContactPerson = x.ContactPerson,
        ContactEmail = x.ContactEmail,
        ContactPhone = x.ContactPhone,
        PlantType = (int)x.PlantType,
        IndustryType = (int)x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Remarks = x.Remarks,
        Status = (int)x.Status,
        ProjectId = x.Project?.Id
    };

    private static StreamInputDto ToDto(WastewaterStream x) => new()
    {
        Id = x.Id,
        EnquiryId = x.EnquiryId,
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