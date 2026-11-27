using EPCApp.Business.Contracts;
using EPCApp.Business.Resolvers;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Engine.Costing;
using EPCApp.Engine.Models;
using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;
using EPCApp.Repositories.Contracts;
using System.Text.Json;

namespace EPCApp.Business.Services;

public sealed class ProposalService : IProposalService
{
    private readonly IEnquiryRepository _enquiries;
    private readonly IProjectRepository _projects;
    private readonly IProposalRepository _proposals;
    private readonly RulePackResolver _resolver;

    public ProposalService(
        IEnquiryRepository enquiries,
        IProjectRepository projects,
        IProposalRepository proposals,
        RulePackResolver resolver)
    {
        _enquiries = enquiries;
        _projects = projects;
        _proposals = proposals;
        _resolver = resolver;
    }

    public async Task<ProposalDto> CalculateAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var enquiry = await _enquiries.GetWithDetailsAsync(enquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {enquiryId} not found.");

        var streams = enquiry.Streams.Where(x => !x.IsDeleted).ToList();
        if (streams.Count == 0)
            throw new InvalidOperationException("Add at least one wastewater stream before calculating.");

        var basis = BuildBasis(enquiry, streams);
        var pack = _resolver.Resolve(enquiry.PlantType);

        TrainDefinition train;
        IReadOnlyList<SizedEquipment> sized;
        IReadOnlyList<BoqLineItem> boq;

        try
        {
            train = pack.SelectTrain(basis);
            sized = pack.Size(basis, train);
            boq = pack.BuildBoq(sized, new RateSheet());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new InvalidOperationException($"Check the stream values. Invalid input: {ex.ParamName}.", ex);
        }

        var proposal = new Proposal
        {
            EnquiryId = enquiryId,
            ProjectId = enquiry.Project?.Id,
            ProposalNo = await _proposals.GenerateProposalNoAsync(enquiryId, cancellationToken),
            VersionNo = await _proposals.GetNextVersionNoAsync(enquiryId, cancellationToken)
        };

        if (enquiry.Status != EnquiryStatus.Won)
            enquiry.Status = EnquiryStatus.ProposalPrepared;

        await _proposals.AddAsync(proposal, cancellationToken);
        await _proposals.SaveChangesAsync(cancellationToken);

        var trainEntity = new TreatmentTrain
        {
            TrainName = train.Name,
            IsUserOverride = false
        };

        var sequence = 1;
        foreach (var operation in train.Operations)
        {
            trainEntity.UnitOperations.Add(new UnitOperation
            {
                SequenceNo = sequence++,
                UnitType = operation
            });
        }

        var itemsByTag = new Dictionary<string, EquipmentItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var equipment in sized)
        {
            var unitOperation = trainEntity.UnitOperations.FirstOrDefault(x => x.UnitType == equipment.Type);
            if (unitOperation is null) continue;

            var item = new EquipmentItem
            {
                TagNo = equipment.Tag,
                Description = equipment.Description,
                Material = equipment.Material,
                ParametersJson = JsonSerializer.Serialize(equipment.Parameters)
            };

            unitOperation.EquipmentItems.Add(item);
            itemsByTag[equipment.Tag] = item;
        }

        var boqEntities = boq.Select(line => new BoqLine
        {
            TagNo = line.Tag,
            ItemDescription = line.Item,
            Unit = line.Unit,
            Quantity = (decimal)line.Quantity,
            Rate = (decimal)line.Rate,
            EquipmentItem = itemsByTag.TryGetValue(line.Tag, out var linked) ? linked : null
        }).ToList();

        await _proposals.ReplaceCalculationAsync(proposal.Id, new[] { trainEntity }, boqEntities, cancellationToken);

        var full = await _proposals.GetFullAsync(proposal.Id, cancellationToken)
            ?? throw new InvalidOperationException("Proposal could not be loaded after calculation.");

        return ToDto(full);
    }

    public async Task<bool> SendAsync(long proposalId, DateTime? validUntil = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null) return false;

        if (proposal.Status != ProposalStatus.Calculated)
            throw new InvalidOperationException("Only a calculated proposal can be sent.");

        if (validUntil.HasValue && validUntil.Value.Date < DateTime.Today)
            throw new InvalidOperationException("Validity date cannot be in the past.");

        var enquiry = await _enquiries.GetByIdAsync(proposal.EnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {proposal.EnquiryId} not found.");

        if (enquiry.Status == EnquiryStatus.Won)
            throw new InvalidOperationException("This enquiry is already won.");

        proposal.Status = ProposalStatus.Sent;
        proposal.SentAt = DateTime.UtcNow;
        proposal.ValidUntil = validUntil?.Date;
        enquiry.Status = EnquiryStatus.ProposalSent;

        await _proposals.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<long> AcceptAsync(long proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        if (proposal.Status != ProposalStatus.Sent)
            throw new InvalidOperationException("Only a sent proposal can be accepted.");

        if (proposal.ValidUntil.HasValue && proposal.ValidUntil.Value.Date < DateTime.Today)
            throw new InvalidOperationException("This proposal has expired. Calculate and send a new version.");

        var enquiry = await _enquiries.GetWithDetailsAsync(proposal.EnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {proposal.EnquiryId} not found.");

        if (enquiry.Status == EnquiryStatus.Won
            || await _projects.GetByEnquiryIdAsync(enquiry.Id, cancellationToken) is not null)
            throw new InvalidOperationException("A project already exists for this enquiry.");

        var project = new Project
        {
            Enquiry = enquiry,
            SourceProposal = proposal,
            ProjectCode = await _projects.GenerateProjectCodeAsync(enquiry.PlantType, cancellationToken),
            Name = enquiry.Title,
            ClientName = enquiry.ClientName,
            PlantType = enquiry.PlantType,
            IndustryType = enquiry.IndustryType,
            ZldRequired = enquiry.ZldRequired,
            SiteLocation = enquiry.SiteLocation,
            Remarks = enquiry.Remarks
        };

        proposal.Status = ProposalStatus.Accepted;
        proposal.AcceptedAt = DateTime.UtcNow;
        enquiry.Status = EnquiryStatus.Won;

        foreach (var version in enquiry.Proposals)
            version.Project = project;

        foreach (var stream in enquiry.Streams)
            stream.Project = project;

        await _projects.AddAsync(project, cancellationToken);
        await _projects.SaveChangesAsync(cancellationToken);

        return project.Id;
    }

    public async Task<bool> RejectAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null) return false;

        if (proposal.Status != ProposalStatus.Sent)
            throw new InvalidOperationException("Only a sent proposal can be rejected.");

        var enquiry = await _enquiries.GetByIdAsync(proposal.EnquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {proposal.EnquiryId} not found.");

        proposal.Status = ProposalStatus.Rejected;
        proposal.RejectedAt = DateTime.UtcNow;
        proposal.Remarks = remarks?.Trim();

        if (enquiry.Status != EnquiryStatus.Won)
            enquiry.Status = EnquiryStatus.ProposalPrepared;

        await _proposals.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetByEnquiryAsync(enquiryId, cancellationToken);
        return rows.Select(ToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetByProjectAsync(projectId, cancellationToken);
        return rows.Select(ToSummaryDto).ToList();
    }

    public async Task<ProposalDto?> GetByIdAsync(long proposalId, CancellationToken cancellationToken = default)
    {
        var full = await _proposals.GetFullAsync(proposalId, cancellationToken);
        return full is null ? null : ToDto(full);
    }

    public async Task<ProposalResultViewModel?> GetResultAsync(long proposalId, CancellationToken cancellationToken = default)
    {
        var full = await _proposals.GetFullAsync(proposalId, cancellationToken);
        if (full is null) return null;

        var dto = ToDto(full);

        return new ProposalResultViewModel
        {
            ProposalId = full.Id,
            EnquiryId = full.EnquiryId,
            ProjectId = full.ProjectId,
            EnquiryNo = full.Enquiry.EnquiryNo,
            ProposalNo = full.ProposalNo,
            VersionNo = full.VersionNo,
            Status = (int)full.Status,
            StatusName = full.Status.ToString(),
            ProjectName = full.Project?.Name ?? full.Enquiry.Title,
            ClientName = full.Enquiry.ClientName,
            PlantTypeName = full.Enquiry.PlantType.ToString().ToUpperInvariant(),
            CurrencyCode = full.CurrencyCode,
            TotalCost = full.TotalCost,
            ValidUntil = full.ValidUntil,
            SentAt = full.SentAt,
            AcceptedAt = full.AcceptedAt,
            RejectedAt = full.RejectedAt,
            Remarks = full.Remarks,
            TrainOperations = dto.TrainOperations,
            Equipment = dto.Equipment,
            BoqLines = dto.BoqLines
        };
    }

    private static ProposalDto ToSummaryDto(Proposal x) => new()
    {
        Id = x.Id,
        EnquiryId = x.EnquiryId,
        ProjectId = x.ProjectId,
        ProposalNo = x.ProposalNo,
        VersionNo = x.VersionNo,
        Status = (int)x.Status,
        CurrencyCode = x.CurrencyCode,
        TotalCost = x.TotalCost,
        ValidUntil = x.ValidUntil,
        SentAt = x.SentAt,
        AcceptedAt = x.AcceptedAt,
        RejectedAt = x.RejectedAt,
        Remarks = x.Remarks
    };
    

    private static StreamBasis BuildBasis(Enquiry enquiry, IReadOnlyList<WastewaterStream> streams)
    {
        var totalFlow = streams.Sum(x => (double)x.FlowM3PerDay);
        if (totalFlow <= 0)
            throw new InvalidOperationException("Total flow must be greater than zero.");

        double Weighted(Func<WastewaterStream, decimal> selector)
            => streams.Sum(x => (double)x.FlowM3PerDay * (double)selector(x)) / totalFlow;

        return new StreamBasis(
            FlowM3PerDay: totalFlow,
            CodMgL: Weighted(x => x.CodMgL),
            BodMgL: Weighted(x => x.BodMgL),
            TssMgL: Weighted(x => x.TssMgL),
            TdsMgL: Weighted(x => x.TdsMgL),
            OilGreaseMgL: Weighted(x => x.OilGreaseMgL),
            Ph: Weighted(x => x.Ph),
            TemperatureC: Weighted(x => x.TemperatureC),
            Industry: enquiry.IndustryType,
            ZldRequired: enquiry.ZldRequired);
    }

    private static ProposalDto ToDto(Proposal proposal)
    {
        var operations = proposal.Trains
            .SelectMany(t => t.UnitOperations)
            .OrderBy(u => u.SequenceNo)
            .ToList();

        return new ProposalDto
        {
            Id = proposal.Id,
            EnquiryId = proposal.EnquiryId,
            ProjectId = proposal.ProjectId,
            ProposalNo = proposal.ProposalNo,
            VersionNo = proposal.VersionNo,
            Status = (int)proposal.Status,
            CurrencyCode = proposal.CurrencyCode,
            TotalCost = proposal.TotalCost,
            ValidUntil = proposal.ValidUntil,
            SentAt = proposal.SentAt,
            AcceptedAt = proposal.AcceptedAt,
            RejectedAt = proposal.RejectedAt,
            Remarks = proposal.Remarks,
            Equipment = operations
                .SelectMany(u => u.EquipmentItems.Select(e => new EquipmentDto
                {
                    Id = e.Id,
                    SequenceNo = u.SequenceNo,
                    UnitType = (int)u.UnitType,
                    UnitTypeName = u.UnitType.ToString(),
                    TagNo = e.TagNo,
                    Description = e.Description,
                    Material = (int)e.Material,
                    MaterialName = e.Material.ToString(),
                    Parameters = string.IsNullOrWhiteSpace(e.ParametersJson)
                        ? new Dictionary<string, decimal>()
                        : JsonSerializer.Deserialize<Dictionary<string, decimal>>(e.ParametersJson)
                          ?? new Dictionary<string, decimal>()
                }))
                .ToList(),
            BoqLines = proposal.BoqLines
                .OrderBy(b => b.Id)
                .Select(b => new BoqLineDto
                {
                    Id = b.Id,
                    TagNo = b.TagNo,
                    ItemDescription = b.ItemDescription,
                    Unit = b.Unit,
                    Quantity = b.Quantity,
                    Rate = b.Rate,
                    Amount = b.Amount
                })
                .ToList()
        };
    }
}