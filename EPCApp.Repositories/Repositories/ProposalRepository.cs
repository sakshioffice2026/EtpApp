using EPCApp.Database.Context;
using EPCApp.Database.Entities;
using EPCApp.Database.Enums;
using EPCApp.Repositories.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EPCApp.Repositories.Repositories;

public class ProposalRepository : GenericRepository<Proposal>, IProposalRepository
{
    public ProposalRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Proposal>> GetListAsync(ProposalStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Enquiry)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Proposal>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Proposal>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

    public async Task<Proposal?> GetFullAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(x => x.Project)
            .Include(x => x.Enquiry)
                .ThenInclude(e => e!.Streams)
            .Include(x => x.Trains)
                .ThenInclude(t => t.UnitOperations)
                    .ThenInclude(u => u.EquipmentItems)
            .Include(x => x.BoqLines)
            .Include(x => x.LineItems)
            .Include(x => x.StatusHistory)
            .Include(x => x.Drawings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Proposal?> GetLatestByProjectAsync(long projectId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Proposal?> GetLatestByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
        => await DbSet
            .Where(x => x.EnquiryId == enquiryId)
            .OrderByDescending(x => x.VersionNo)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetNextVersionNoAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var latest = await DbSet
            .Where(x => x.ProjectId == projectId)
            .MaxAsync(x => (int?)x.VersionNo, cancellationToken);

        return (latest ?? 0) + 1;
    }

    public async Task<int> GetNextVersionNoByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var latest = await DbSet
            .Where(x => x.EnquiryId == enquiryId)
            .MaxAsync(x => (int?)x.VersionNo, cancellationToken);

        return (latest ?? 0) + 1;
    }

    public async Task<string> GenerateProposalNoAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .Select(x => x.ProposalNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(existing))
            return existing;

        var projectCode = await Context.Set<Project>()
            .AsNoTracking()
            .Where(x => x.Id == projectId)
            .Select(x => x.ProjectCode)
            .FirstOrDefaultAsync(cancellationToken);

        return $"PR-{projectCode ?? projectId.ToString()}";
    }

    public async Task<string> GenerateProposalNoForEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId)
            .Select(x => x.ProposalNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(existing))
            return existing;

        var enquiryNo = await Context.Set<Enquiry>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == enquiryId)
            .Select(x => x.EnquiryNo)
            .FirstOrDefaultAsync(cancellationToken);

        return $"PR-{enquiryNo ?? enquiryId.ToString()}";
    }

    public Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        CancellationToken cancellationToken = default)
        => ReplaceCalculationAsync(proposalId, trains, boqLines, Array.Empty<ProposalLineItem>(), cancellationToken);

    public async Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        IEnumerable<ProposalLineItem> lineItems,
        CancellationToken cancellationToken = default)
    {
        var proposal = await DbSet.FirstOrDefaultAsync(x => x.Id == proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        var trainList = trains.ToList();
        var boqList = boqLines.ToList();
        var lineList = lineItems.ToList();

        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        await Context.Set<ProposalLineItem>()
            .Where(x => x.ProposalId == proposalId)
            .ExecuteDeleteAsync(cancellationToken);

        await Context.Set<BoqLine>()
            .Where(x => x.ProposalId == proposalId)
            .ExecuteDeleteAsync(cancellationToken);

        await Context.Set<TreatmentTrain>()
            .Where(x => x.ProposalId == proposalId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var train in trainList)
            train.ProposalId = proposalId;

        foreach (var line in boqList)
            line.ProposalId = proposalId;

        foreach (var item in lineList)
            item.ProposalId = proposalId;

        await Context.Set<TreatmentTrain>().AddRangeAsync(trainList, cancellationToken);
        await Context.Set<BoqLine>().AddRangeAsync(boqList, cancellationToken);
        await Context.Set<ProposalLineItem>().AddRangeAsync(lineList, cancellationToken);

        var previous = proposal.Status;

        proposal.TotalCost = lineList.Count > 0
            ? Math.Round(lineList.Sum(x => x.Quantity * x.Rate), 2)
            : Math.Round(boqList.Sum(x => x.Quantity * x.Rate), 2);
        proposal.Status = ProposalStatus.Calculated;
        proposal.UpdatedAt = DateTime.UtcNow;

        await Context.Set<ProposalStatusHistory>().AddAsync(new ProposalStatusHistory
        {
            ProposalId = proposalId,
            FromStatus = previous,
            ToStatus = ProposalStatus.Calculated,
            Remarks = "Calculated"
        }, cancellationToken);

        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetStatusAsync(
        long proposalId,
        ProposalStatus newStatus,
        string? changedBy = null,
        string? remarks = null,
        CancellationToken cancellationToken = default)
    {
        var proposal = await DbSet.FirstOrDefaultAsync(x => x.Id == proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        var previous = proposal.Status;

        proposal.Status = newStatus;
        proposal.UpdatedAt = DateTime.UtcNow;

        await Context.Set<ProposalStatusHistory>().AddAsync(new ProposalStatusHistory
        {
            ProposalId = proposalId,
            FromStatus = previous,
            ToStatus = newStatus,
            ChangedBy = changedBy,
            Remarks = remarks
        }, cancellationToken);

        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalStatusHistory>> GetHistoryAsync(long proposalId, CancellationToken cancellationToken = default)
        => await Context.Set<ProposalStatusHistory>()
            .AsNoTracking()
            .Where(x => x.ProposalId == proposalId)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
}