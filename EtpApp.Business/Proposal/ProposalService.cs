using System.Text.Json;
using EtpApp.Business.Contracts;
using EtpApp.Business.Resolvers;
using EtpApp.Database.Entities;
using EtpApp.Engine.Costing;
using EtpApp.Engine.Models;
using EtpApp.Model.DTOs;
using EtpApp.Model.ViewModel;
using EtpApp.Repositories.Contracts;

namespace EtpApp.Business.Services;

public sealed class ProposalService : IProposalService
{
    private readonly IProjectRepository _projects;
    private readonly IProposalRepository _proposals;
    private readonly RulePackResolver _resolver;

    public ProposalService(
        IProjectRepository projects,
        IProposalRepository proposals,
        RulePackResolver resolver)
    {
        _projects = projects;
        _proposals = proposals;
        _resolver = resolver;
    }

    public async Task<ProposalDto> CalculateAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetWithStreamsAsync(projectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");

        var streams = project.Streams.Where(x => !x.IsDeleted).ToList();
        if (streams.Count == 0)
            throw new InvalidOperationException("Add at least one wastewater stream before calculating.");

        var basis = BuildBasis(project, streams);
        var pack = _resolver.Resolve(project.PlantType);

        var train = pack.SelectTrain(basis);
        var sized = pack.Size(basis, train);
        var boq = pack.BuildBoq(sized, new RateSheet());

        var proposal = new Proposal
        {
            ProjectId = projectId,
            ProposalNo = await _proposals.GenerateProposalNoAsync(projectId, cancellationToken),
            VersionNo = await _proposals.GetNextVersionNoAsync(projectId, cancellationToken)
        };

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

    public async Task<IReadOnlyList<ProposalDto>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _proposals.GetByProjectAsync(projectId, cancellationToken);

        return rows.Select(x => new ProposalDto
        {
            Id = x.Id,
            ProjectId = x.ProjectId,
            ProposalNo = x.ProposalNo,
            VersionNo = x.VersionNo,
            Status = (int)x.Status,
            CurrencyCode = x.CurrencyCode,
            TotalCost = x.TotalCost
        }).ToList();
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
            ProjectName = full.Project.Name,
            ClientName = full.Project.ClientName,
            PlantTypeName = full.Project.PlantType.ToString().ToUpperInvariant(),
            CurrencyCode = full.CurrencyCode,
            TotalCost = full.TotalCost,
            TrainOperations = dto.TrainOperations,
            Equipment = dto.Equipment,
            BoqLines = dto.BoqLines
        };
    }

    private static StreamBasis BuildBasis(Project project, IReadOnlyList<WastewaterStream> streams)
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
            Industry: project.IndustryType,
            ZldRequired: project.ZldRequired);
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
            ProjectId = proposal.ProjectId,
            ProposalNo = proposal.ProposalNo,
            VersionNo = proposal.VersionNo,
            Status = (int)proposal.Status,
            CurrencyCode = proposal.CurrencyCode,
            TotalCost = proposal.TotalCost,
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