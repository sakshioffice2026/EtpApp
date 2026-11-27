using EPCApp.Business.Contracts;
using EPCApp.Business.Resolvers;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Engine.Costing;
using EPCApp.Engine.Models;
using EPCApp.Model.DTOs;
using EPCApp.Model.ViewModel;
using EPCApp.Repositories.Contracts;
using System.Numerics;
using System.Text.Json;

namespace EPCApp.Business.Services;

public sealed class ProposalService : IProposalService
{
    private readonly IProjectRepository _projects;
    private readonly IEnquiryRepository _enquiries;
    private readonly RulePackResolver _resolver;
    private readonly IDesignBasisLoader _designBasisLoader;
    private readonly IProposalRepository _proposals;

    public ProposalService(
        IProjectRepository projects,
        IEnquiryRepository enquiries,
        IProposalRepository proposals,
        RulePackResolver resolver,
        IDesignBasisLoader designBasisLoader)
    {
        _projects = projects;
        _enquiries = enquiries;
        _proposals = proposals;
        _resolver = resolver;
        _designBasisLoader = designBasisLoader;
    }

    private sealed record StreamValues(
        decimal Flow,
        decimal Cod,
        decimal Bod,
        decimal Tss,
        decimal Tds,
        decimal Oil,
        decimal Ph,
        decimal Temperature);

    public async Task<ProposalDto> CalculateAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetWithStreamsAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var values = project.Streams
            .Where(x => !x.IsDeleted)
            .Select(x => new StreamValues(x.FlowM3PerDay, x.CodMgL, x.BodMgL, x.TssMgL, x.TdsMgL, x.OilGreaseMgL, x.Ph, x.TemperatureC))
            .ToList();

        var basis = BuildBasis(project.IndustryType, project.ZldRequired, values);

        var proposal = new Proposal
        {
            ProjectId = projectId,
            ProposalNo = await _proposals.GenerateProposalNoAsync(projectId, cancellationToken),
            VersionNo = await _proposals.GetNextVersionNoAsync(projectId, cancellationToken)
        };

        await _proposals.AddAsync(proposal, cancellationToken);
        await _proposals.SaveChangesAsync(cancellationToken);

        await CalculateIntoAsync(proposal.Id, project.PlantType, basis, cancellationToken);

        return await LoadDtoAsync(proposal.Id, cancellationToken);
    }

    public async Task<ProposalDto> CreateFromEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var enquiry = await _enquiries.GetWithStreamsAsync(enquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {enquiryId} not found.");

        var basis = BuildBasis(enquiry.IndustryType, enquiry.ZldRequired, ToValues(enquiry));

        var proposal = new Proposal
        {
            EnquiryId = enquiryId,
            ProposalNo = await _proposals.GenerateProposalNoForEnquiryAsync(enquiryId, cancellationToken),
            VersionNo = await _proposals.GetNextVersionNoByEnquiryAsync(enquiryId, cancellationToken)
        };

        await _proposals.AddAsync(proposal, cancellationToken);
        await _proposals.SaveChangesAsync(cancellationToken);

        await CalculateIntoAsync(proposal.Id, enquiry.PlantType, basis, cancellationToken);

        if (enquiry.Status is EnquiryStatus.Draft or EnquiryStatus.Submitted)
        {
            enquiry.Status = EnquiryStatus.ProposalCreated;
            await _enquiries.SaveChangesAsync(cancellationToken);
        }

        return await LoadDtoAsync(proposal.Id, cancellationToken);
    }

    public async Task<ProposalDto> RecalculateAsync(long proposalId, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        Require(proposal, "recalculate", ProposalStatus.Draft, ProposalStatus.Calculated);

        if (proposal.EnquiryId.HasValue)
        {
            var enquiry = await _enquiries.GetWithStreamsAsync(proposal.EnquiryId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Enquiry {proposal.EnquiryId} not found.");

            var basis = BuildBasis(enquiry.IndustryType, enquiry.ZldRequired, ToValues(enquiry));
            await CalculateIntoAsync(proposal.Id, enquiry.PlantType, basis, cancellationToken);
        }
        else if (proposal.ProjectId.HasValue)
        {
            var project = await _projects.GetWithStreamsAsync(proposal.ProjectId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Project {proposal.ProjectId} not found.");

            var values = project.Streams
                .Where(x => !x.IsDeleted)
                .Select(x => new StreamValues(x.FlowM3PerDay, x.CodMgL, x.BodMgL, x.TssMgL, x.TdsMgL, x.OilGreaseMgL, x.Ph, x.TemperatureC))
                .ToList();

            var basis = BuildBasis(project.IndustryType, project.ZldRequired, values);
            await CalculateIntoAsync(proposal.Id, project.PlantType, basis, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Proposal has no enquiry or project to calculate from.");
        }

        return await LoadDtoAsync(proposalId, cancellationToken);
    }

    public async Task SendAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        Require(proposal, "send", ProposalStatus.Calculated);

        await _proposals.SetStatusAsync(proposalId, ProposalStatus.Submitted, null, remarks ?? "Sent to client", cancellationToken);

        if (proposal.EnquiryId.HasValue)
        {
            var enquiry = await _enquiries.GetByIdAsync(proposal.EnquiryId.Value, cancellationToken);
            if (enquiry is not null && enquiry.Status is EnquiryStatus.Draft)
            {
                enquiry.Status = EnquiryStatus.Submitted;
                await _enquiries.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<long> AcceptAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        Require(proposal, "accept", ProposalStatus.Submitted);

        if (proposal.ProjectId.HasValue)
        {
            await _proposals.SetStatusAsync(proposalId, ProposalStatus.Approved, null, remarks ?? "Accepted", cancellationToken);
            return proposal.ProjectId.Value;
        }

        if (!proposal.EnquiryId.HasValue)
            throw new InvalidOperationException("Proposal has no enquiry to create a project from.");

        var enquiry = await _enquiries.GetWithStreamsAsync(proposal.EnquiryId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {proposal.EnquiryId} not found.");

        var project = new Project
        {
            ProjectCode = await _projects.GenerateProjectCodeAsync(enquiry.PlantType, cancellationToken),
            Name = enquiry.ProjectName,
            ClientName = enquiry.ClientName,
            PlantType = enquiry.PlantType,
            IndustryType = enquiry.IndustryType,
            ZldRequired = enquiry.ZldRequired || enquiry.PlantType == PlantType.Zld,
            SiteLocation = enquiry.SiteLocation,
            Remarks = enquiry.Remarks,
            EnquiryId = enquiry.Id,
            SourceProposalId = proposal.Id
        };

        foreach (var stream in enquiry.Streams.Where(x => !x.IsDeleted))
        {
            project.Streams.Add(new WastewaterStream
            {
                StreamName = stream.StreamName,
                FlowM3PerDay = stream.FlowM3PerDay,
                CodMgL = stream.CodMgL,
                BodMgL = stream.BodMgL,
                TssMgL = stream.TssMgL,
                TdsMgL = stream.TdsMgL,
                OilGreaseMgL = stream.OilGreaseMgL,
                Ph = stream.Ph,
                TemperatureC = stream.TemperatureC
            });
        }

        await _projects.AddAsync(project, cancellationToken);
        await _projects.SaveChangesAsync(cancellationToken);

        proposal.ProjectId = project.Id;
        enquiry.Status = EnquiryStatus.Converted;

        await _proposals.SetStatusAsync(proposalId, ProposalStatus.Approved, null, remarks ?? "Accepted", cancellationToken);

        return project.Id;
    }

    public async Task RejectAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        Require(proposal, "reject", ProposalStatus.Submitted);

        await _proposals.SetStatusAsync(proposalId, ProposalStatus.Rejected, null, remarks ?? "Rejected", cancellationToken);
    }

    public async Task<ProposalDto> ReviseAsync(long proposalId, string? remarks = null, CancellationToken cancellationToken = default)
    {
        var proposal = await _proposals.GetByIdAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        Require(proposal, "revise", ProposalStatus.Calculated, ProposalStatus.Submitted, ProposalStatus.Rejected);

        if (!proposal.EnquiryId.HasValue)
            throw new InvalidOperationException("Only proposals created from an enquiry can be revised.");

        var enquiryId = proposal.EnquiryId.Value;

        var enquiry = await _enquiries.GetWithStreamsAsync(enquiryId, cancellationToken)
            ?? throw new InvalidOperationException($"Enquiry {enquiryId} not found.");

        var basis = BuildBasis(enquiry.IndustryType, enquiry.ZldRequired, ToValues(enquiry));

        await _proposals.SetStatusAsync(proposalId, ProposalStatus.Revised, null, remarks ?? "Revised", cancellationToken);

        var revision = new Proposal
        {
            EnquiryId = enquiryId,
            ProposalNo = await _proposals.GenerateProposalNoForEnquiryAsync(enquiryId, cancellationToken),
            VersionNo = await _proposals.GetNextVersionNoByEnquiryAsync(enquiryId, cancellationToken)
        };

        await _proposals.AddAsync(revision, cancellationToken);
        await _proposals.SaveChangesAsync(cancellationToken);

        await CalculateIntoAsync(revision.Id, enquiry.PlantType, basis, cancellationToken);

        return await LoadDtoAsync(revision.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetByProjectAsync(projectId, cancellationToken);
        return rows.Select(ToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetByEnquiryAsync(enquiryId, cancellationToken);
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
            ProposalNo = full.ProposalNo,
            ProjectName = full.Project?.Name ?? full.Enquiry?.ProjectName ?? string.Empty,
            ClientName = full.Project?.ClientName ?? full.Enquiry?.ClientName ?? string.Empty,
            PlantTypeName = (full.Project?.PlantType ?? full.Enquiry?.PlantType ?? PlantType.Etp).ToString().ToUpperInvariant(),
            CurrencyCode = full.CurrencyCode,
            TotalCost = full.TotalCost,
            TrainOperations = dto.TrainOperations,
            Equipment = dto.Equipment,
            BoqLines = dto.BoqLines
        };
    }

    public async Task<ProposalListViewModel> GetListAsync(int? status = null, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetListAsync(status.HasValue ? (ProposalStatus)status.Value : null, cancellationToken);

        return new ProposalListViewModel
        {
            Status = status,
            Items = rows.Select(x => new ProposalListItemViewModel
            {
                Id = x.Id,
                ProposalNo = x.ProposalNo,
                VersionNo = x.VersionNo,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                EnquiryId = x.EnquiryId,
                EnquiryNo = x.Enquiry?.EnquiryNo,
                ProjectId = x.ProjectId,
                ProjectName = x.Project?.Name ?? x.Enquiry?.ProjectName ?? string.Empty,
                ClientName = x.Project?.ClientName ?? x.Enquiry?.ClientName ?? string.Empty,
                PlantTypeName = (x.Project?.PlantType ?? x.Enquiry?.PlantType ?? PlantType.Etp).ToString().ToUpperInvariant(),
                CurrencyCode = x.CurrencyCode,
                TotalCost = x.TotalCost,
                CreatedAt = x.CreatedAt
            }).ToList()
        };
    }

    public async Task<ProposalDetailViewModel?> GetDetailAsync(long proposalId, CancellationToken cancellationToken = default)
    {
        var full = await _proposals.GetFullAsync(proposalId, cancellationToken);
        if (full is null) return null;

        var dto = ToDto(full);

        return new ProposalDetailViewModel
        {
            ProposalId = full.Id,
            ProposalNo = full.ProposalNo,
            VersionNo = full.VersionNo,
            Status = (int)full.Status,
            StatusName = full.Status.ToString(),
            EnquiryId = full.EnquiryId,
            EnquiryNo = full.Enquiry?.EnquiryNo,
            ProjectId = full.ProjectId,
            ProjectName = full.Project?.Name ?? full.Enquiry?.ProjectName ?? string.Empty,
            ClientName = full.Project?.ClientName ?? full.Enquiry?.ClientName ?? string.Empty,
            PlantTypeName = (full.Project?.PlantType ?? full.Enquiry?.PlantType ?? PlantType.Etp).ToString().ToUpperInvariant(),
            Notes = full.Notes,
            CurrencyCode = full.CurrencyCode,
            TotalCost = full.TotalCost,
            CreatedAt = full.CreatedAt,
            CanRecalculate = full.Status is ProposalStatus.Draft or ProposalStatus.Calculated,
            CanSend = full.Status == ProposalStatus.Calculated,
            CanAccept = full.Status == ProposalStatus.Submitted,
            CanReject = full.Status == ProposalStatus.Submitted,
            CanRevise = full.EnquiryId.HasValue
                && full.Status is ProposalStatus.Calculated or ProposalStatus.Submitted or ProposalStatus.Rejected,
            TrainOperations = dto.TrainOperations,
            Equipment = dto.Equipment,
            BoqLines = dto.BoqLines,
            LineItems = dto.LineItems,
            StatusHistory = dto.StatusHistory,
            Streams = full.Enquiry is null
                ? new List<EnquiryStreamDto>()
                : full.Enquiry.Streams
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
        };
    }

    private async Task CalculateIntoAsync(long proposalId, PlantType plantType, StreamBasis basis, CancellationToken cancellationToken)
    {
        var pack = _resolver.Resolve(plantType);
        var design = await _designBasisLoader.LoadAsync(plantType, cancellationToken);
        var plan = new TrainPlan();

        var train = pack.SelectTrain(basis);
        var sized = pack.Size(basis, train, design, plan);
        var boq = pack.BuildBoq(sized, design);
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
            Category = line.Category,
            TagNo = line.Tag,
            ItemDescription = line.Item,
            Unit = line.Unit,
            Quantity = (decimal)line.Quantity,
            Rate = (decimal)line.Rate,
            EquipmentItem = itemsByTag.TryGetValue(line.Tag, out var linked) ? linked : null
        }).ToList();

        var lineItems = BuildLineItems(boqEntities);

        await _proposals.ReplaceCalculationAsync(proposalId, new[] { trainEntity }, boqEntities, lineItems, cancellationToken);

        var notes = plan.Notes.Count == 0 ? null : string.Join(Environment.NewLine, plan.Notes);
        var saved = await _proposals.GetByIdAsync(proposalId, cancellationToken);
        if (saved is not null)
        {
            saved.Notes = notes;
            _proposals.Update(saved);
            await _proposals.SaveChangesAsync(cancellationToken);
        }

    }


    private async Task<ProposalDto> LoadDtoAsync(long proposalId, CancellationToken cancellationToken)
    {
        var full = await _proposals.GetFullAsync(proposalId, cancellationToken)
            ?? throw new InvalidOperationException("Proposal could not be loaded after calculation.");

        return ToDto(full);
    }

    private static List<ProposalLineItem> BuildLineItems(IEnumerable<BoqLine> lines)
        => lines
            .GroupBy(x => x.Category)
            .OrderBy(g => (int)g.Key)
            .Select(g => new ProposalLineItem
            {
                Category = g.Key,
                SortOrder = (int)g.Key,
                Description = $"{g.Key} works",
                Unit = "LS",
                Quantity = 1,
                Rate = Math.Round(g.Sum(x => x.Quantity * x.Rate), 2)
            })
            .Where(x => x.Rate > 0)
            .ToList();

    private static void Require(Proposal proposal, string action, params ProposalStatus[] allowed)
    {
        if (!allowed.Contains(proposal.Status))
            throw new InvalidOperationException($"Cannot {action} a proposal in '{proposal.Status}' status.");
    }

    private static IReadOnlyList<StreamValues> ToValues(Enquiry enquiry)
        => enquiry.Streams
            .Where(x => !x.IsDeleted)
            .Select(x => new StreamValues(x.FlowM3PerDay, x.CodMgL, x.BodMgL, x.TssMgL, x.TdsMgL, x.OilGreaseMgL, x.Ph, x.TemperatureC))
            .ToList();

    private static StreamBasis BuildBasis(IndustryType industry, bool zldRequired, IReadOnlyList<StreamValues> streams)
    {
        if (streams.Count == 0)
            throw new InvalidOperationException("Add at least one wastewater stream before calculating.");

        var totalFlow = streams.Sum(x => (double)x.Flow);
        if (totalFlow <= 0)
            throw new InvalidOperationException("Total flow must be greater than zero.");

        double Weighted(Func<StreamValues, decimal> selector)
            => streams.Sum(x => (double)x.Flow * (double)selector(x)) / totalFlow;

        return new StreamBasis(
            FlowM3PerDay: totalFlow,
            CodMgL: Weighted(x => x.Cod),
            BodMgL: Weighted(x => x.Bod),
            TssMgL: Weighted(x => x.Tss),
            TdsMgL: Weighted(x => x.Tds),
            OilGreaseMgL: Weighted(x => x.Oil),
            Ph: Weighted(x => x.Ph),
            TemperatureC: Weighted(x => x.Temperature),
            Industry: industry,
            ZldRequired: zldRequired);
    }

    private static ProposalDto ToSummaryDto(Proposal x) => new()
    {
        Id = x.Id,
        ProjectId = x.ProjectId,
        EnquiryId = x.EnquiryId,
        ProposalNo = x.ProposalNo,
        VersionNo = x.VersionNo,
        Status = (int)x.Status,
        StatusName = x.Status.ToString(),
        CurrencyCode = x.CurrencyCode,
        TotalCost = x.TotalCost,
        CreatedAt = x.CreatedAt
    };

    private static ProposalDto ToDto(Proposal proposal)
    {
        var operations = proposal.Trains
            .SelectMany(t => t.UnitOperations)
            .OrderBy(u => u.SequenceNo)
            .ToList();

        return new ProposalDto
        {
            Id = proposal.Id,
            ProjectId = proposal.ProjectId,
            EnquiryId = proposal.EnquiryId,
            ProposalNo = proposal.ProposalNo,
            VersionNo = proposal.VersionNo,
            Status = (int)proposal.Status,
            StatusName = proposal.Status.ToString(),
            CurrencyCode = proposal.CurrencyCode,
            TotalCost = proposal.TotalCost,
            CreatedAt = proposal.CreatedAt,
            Notes = proposal.Notes,
            TrainOperations = operations.Select(u => u.UnitType.ToString()).ToList(),
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
                    Category = (int)b.Category,
                    CategoryName = b.Category.ToString(),
                    ItemDescription = b.ItemDescription,
                    Unit = b.Unit,
                    Quantity = b.Quantity,
                    Rate = b.Rate,
                    Amount = b.Amount
                })
                .ToList(),
            LineItems = proposal.LineItems
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new ProposalLineItemDto
                {
                    Id = i.Id,
                    ProposalId = i.ProposalId,
                    Category = (int)i.Category,
                    CategoryName = i.Category.ToString(),
                    SortOrder = i.SortOrder,
                    Description = i.Description,
                    Unit = i.Unit,
                    Quantity = i.Quantity,
                    Rate = i.Rate,
                    Amount = i.Amount
                })
                .ToList(),
            StatusHistory = proposal.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .ThenBy(h => h.Id)
                .Select(h => new ProposalStatusHistoryDto
                {
                    Id = h.Id,
                    ProposalId = h.ProposalId,
                    FromStatus = h.FromStatus.HasValue ? (int)h.FromStatus.Value : null,
                    FromStatusName = h.FromStatus?.ToString(),
                    ToStatus = (int)h.ToStatus,
                    ToStatusName = h.ToStatus.ToString(),
                    ChangedBy = h.ChangedBy,
                    Remarks = h.Remarks,
                    CreatedAt = h.CreatedAt
                })
                .ToList()
        };
    }
}
