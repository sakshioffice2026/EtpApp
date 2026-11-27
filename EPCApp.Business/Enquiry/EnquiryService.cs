using EPCApp.Business.Contracts;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Model.DTOs;
using EPCApp.Repositories.Contracts;

namespace EPCApp.Business.Services;

public sealed class EnquiryService : IEnquiryService
{
    private readonly IEnquiryRepository _enquiries;

    public EnquiryService(IEnquiryRepository enquiries)
    {
        _enquiries = enquiries;
    }

    public async Task<IReadOnlyList<EnquiryDto>> GetAllAsync(int? plantType = null, int? status = null, CancellationToken cancellationToken = default)
    {
        var rows = await _enquiries.GetActiveAsync(
            plantType.HasValue ? (PlantType)plantType.Value : null,
            status.HasValue ? (EnquiryStatus)status.Value : null,
            cancellationToken);

        return rows.Select(x => ToDto(x, includeStreams: false)).ToList();
    }

    public async Task<EnquiryDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetWithStreamsAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity, includeStreams: true);
    }

    public Task<string> GenerateEnquiryNoAsync(int plantType, CancellationToken cancellationToken = default)
        => _enquiries.GenerateEnquiryNoAsync((PlantType)plantType, cancellationToken);

    public async Task<EnquiryDto> CreateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default)
    {
        var entity = new Enquiry
        {
            EnquiryNo = string.IsNullOrWhiteSpace(enquiry.EnquiryNo)
                ? await _enquiries.GenerateEnquiryNoAsync((PlantType)enquiry.PlantType, cancellationToken)
                : enquiry.EnquiryNo.Trim(),
            ClientName = enquiry.ClientName.Trim(),
            ContactPerson = enquiry.ContactPerson?.Trim(),
            Email = enquiry.Email?.Trim(),
            Phone = enquiry.Phone?.Trim(),
            ProjectName = enquiry.ProjectName.Trim(),
            PlantType = (PlantType)enquiry.PlantType,
            IndustryType = (IndustryType)enquiry.IndustryType,
            ZldRequired = enquiry.ZldRequired || enquiry.PlantType == (int)PlantType.Zld,
            SiteLocation = enquiry.SiteLocation?.Trim(),
            Remarks = enquiry.Remarks?.Trim(),
            Status = EnquiryStatus.Draft
        };

        foreach (var stream in enquiry.Streams.Where(HasContent))
            entity.Streams.Add(ToEntity(stream));

        await _enquiries.AddAsync(entity, cancellationToken);
        await _enquiries.SaveChangesAsync(cancellationToken);

        return ToDto(entity, includeStreams: true);
    }

    public async Task<bool> UpdateAsync(EnquiryDto enquiry, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(enquiry.Id, cancellationToken);
        if (entity is null) return false;

        entity.ClientName = enquiry.ClientName.Trim();
        entity.ContactPerson = enquiry.ContactPerson?.Trim();
        entity.Email = enquiry.Email?.Trim();
        entity.Phone = enquiry.Phone?.Trim();
        entity.ProjectName = enquiry.ProjectName.Trim();
        entity.PlantType = (PlantType)enquiry.PlantType;
        entity.IndustryType = (IndustryType)enquiry.IndustryType;
        entity.ZldRequired = enquiry.ZldRequired || enquiry.PlantType == (int)PlantType.Zld;
        entity.SiteLocation = enquiry.SiteLocation?.Trim();
        entity.Remarks = enquiry.Remarks?.Trim();

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);

        var streams = enquiry.Streams.Where(HasContent).Select(ToEntity).ToList();
        await _enquiries.ReplaceStreamsAsync(entity.Id, streams, cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        entity.IsDeleted = true;

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetStatusAsync(long id, int status, CancellationToken cancellationToken = default)
    {
        var entity = await _enquiries.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;

        entity.Status = (EnquiryStatus)status;

        _enquiries.Update(entity);
        await _enquiries.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool HasContent(EnquiryStreamDto stream)
        => !string.IsNullOrWhiteSpace(stream.StreamName) || stream.FlowM3PerDay > 0;

    private static EnquiryStream ToEntity(EnquiryStreamDto x) => new()
    {
        StreamName = string.IsNullOrWhiteSpace(x.StreamName) ? "Stream" : x.StreamName.Trim(),
        FlowM3PerDay = x.FlowM3PerDay,
        CodMgL = x.CodMgL,
        BodMgL = x.BodMgL,
        TssMgL = x.TssMgL,
        TdsMgL = x.TdsMgL,
        OilGreaseMgL = x.OilGreaseMgL,
        Ph = x.Ph,
        TemperatureC = x.TemperatureC
    };

    private static EnquiryDto ToDto(Enquiry x, bool includeStreams) => new()
    {
        Id = x.Id,
        EnquiryNo = x.EnquiryNo,
        ClientName = x.ClientName,
        ContactPerson = x.ContactPerson,
        Email = x.Email,
        Phone = x.Phone,
        ProjectName = x.ProjectName,
        PlantType = (int)x.PlantType,
        IndustryType = (int)x.IndustryType,
        ZldRequired = x.ZldRequired,
        SiteLocation = x.SiteLocation,
        Status = (int)x.Status,
        Remarks = x.Remarks,
        CreatedAt = x.CreatedAt,
        Streams = includeStreams
            ? x.Streams
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Id)
                .Select(s => new EnquiryStreamDto
                {
                    Id = s.Id,
                    EnquiryId = s.EnquiryId,
                    StreamName = s.StreamName,
                    FlowM3PerDay = s.FlowM3PerDay,
                    CodMgL = s.CodMgL,
                    BodMgL = s.BodMgL,
                    TssMgL = s.TssMgL,
                    TdsMgL = s.TdsMgL,
                    OilGreaseMgL = s.OilGreaseMgL,
                    Ph = s.Ph,
                    TemperatureC = s.TemperatureC
                })
                .ToList()
            : new List<EnquiryStreamDto>()
    };
}
