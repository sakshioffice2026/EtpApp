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
    public async Task<IReadOnlyList<Proposal>> GetByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    => await DbSet
        .AsNoTracking()
        .Where(x => x.EnquiryId == enquiryId)
        .OrderByDescending(x => x.VersionNo)
        .ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Proposal>> GetByProjectAsync(long projectId, CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

    public async Task<Proposal?> GetFullAsync(long id, CancellationToken cancellationToken = default)
        => await DbSet
             .Include(x => x.Enquiry)
            .Include(x => x.Project)
            .Include(x => x.Trains)
                .ThenInclude(t => t.UnitOperations)
                    .ThenInclude(u => u.EquipmentItems)
            .Include(x => x.BoqLines)
            .Include(x => x.Drawings)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Proposal?> GetLatestByEnquiryAsync(long enquiryId, CancellationToken cancellationToken = default)
    => await DbSet
        .Where(x => x.EnquiryId == enquiryId)
        .OrderByDescending(x => x.VersionNo)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<int> GetNextVersionNoAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var latest = await DbSet
            .Where(x => x.EnquiryId == enquiryId)
            .MaxAsync(x => (int?)x.VersionNo, cancellationToken);

        return (latest ?? 0) + 1;
    }

    public async Task<string> GenerateProposalNoAsync(long enquiryId, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .AsNoTracking()
            .Where(x => x.EnquiryId == enquiryId)
            .Select(x => x.ProposalNo)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(existing))
            return existing;

        var enquiryNo = await Context.Set<Enquiry>()
            .AsNoTracking()
            .Where(x => x.Id == enquiryId)
            .Select(x => x.EnquiryNo)
            .FirstOrDefaultAsync(cancellationToken);

        return $"PR-{enquiryNo ?? enquiryId.ToString()}";
    }

    public async Task ReplaceCalculationAsync(
        long proposalId,
        IEnumerable<TreatmentTrain> trains,
        IEnumerable<BoqLine> boqLines,
        CancellationToken cancellationToken = default)
    {
        var proposal = await DbSet.FirstOrDefaultAsync(x => x.Id == proposalId, cancellationToken)
            ?? throw new InvalidOperationException($"Proposal {proposalId} not found.");

        var trainList = trains.ToList();
        var boqList = boqLines.ToList();

        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

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

        await Context.Set<TreatmentTrain>().AddRangeAsync(trainList, cancellationToken);
        await Context.Set<BoqLine>().AddRangeAsync(boqList, cancellationToken);

        proposal.TotalCost = Math.Round(boqList.Sum(x => x.Quantity * x.Rate), 2);
        proposal.Status = ProposalStatus.Calculated;
        proposal.UpdatedAt = DateTime.UtcNow;

        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}