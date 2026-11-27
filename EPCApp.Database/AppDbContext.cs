using EPCApp.Database.Entities;
using Microsoft.EntityFrameworkCore;
namespace EPCApp.Database.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<EnquiryStream> EnquiryStreams => Set<EnquiryStream>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WastewaterStream> WastewaterStreams => Set<WastewaterStream>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<ProposalLineItem> ProposalLineItems => Set<ProposalLineItem>();
    public DbSet<ProposalStatusHistory> ProposalStatusHistories => Set<ProposalStatusHistory>();
    public DbSet<TreatmentTrain> TreatmentTrains => Set<TreatmentTrain>();
    public DbSet<UnitOperation> UnitOperations => Set<UnitOperation>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<BoqLine> BoqLines => Set<BoqLine>();
    public DbSet<DrawingRecord> DrawingRecords => Set<DrawingRecord>();
    public DbSet<DesignRule> DesignRules => Set<DesignRule>();
    public DbSet<RateCard> RateCards => Set<RateCard>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Enquiry>(e =>
        {
            e.HasIndex(x => x.EnquiryNo).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.PlantType);
            e.Property(x => x.EnquiryNo).HasMaxLength(30);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.ContactPerson).HasMaxLength(150);
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.ProjectName).HasMaxLength(200);
            e.Property(x => x.SiteLocation).HasMaxLength(250);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.PlantType).HasConversion<byte>();
            e.Property(x => x.IndustryType).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<EnquiryStream>(e =>
        {
            e.Property(x => x.StreamName).HasMaxLength(150);
            e.Property(x => x.FlowM3PerDay).HasColumnName("flow_m3_per_day").HasPrecision(12, 2);
            e.Property(x => x.CodMgL).HasPrecision(12, 2);
            e.Property(x => x.BodMgL).HasPrecision(12, 2);
            e.Property(x => x.TssMgL).HasPrecision(12, 2);
            e.Property(x => x.TdsMgL).HasPrecision(12, 2);
            e.Property(x => x.OilGreaseMgL).HasPrecision(12, 2);
            e.Property(x => x.Ph).HasPrecision(4, 2);
            e.Property(x => x.TemperatureC).HasPrecision(5, 2);
            e.HasOne(x => x.Enquiry)
                .WithMany(p => p.Streams)
                .HasForeignKey(x => x.EnquiryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(x => x.ProjectCode).IsUnique();
            e.Property(x => x.ProjectCode).HasMaxLength(30);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.SiteLocation).HasMaxLength(250);
            e.Property(x => x.Remarks).HasMaxLength(1000);
            e.Property(x => x.PlantType).HasConversion<byte>();
            e.Property(x => x.IndustryType).HasConversion<byte>();
            e.HasOne(x => x.Enquiry)
                .WithMany()
                .HasForeignKey(x => x.EnquiryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.SourceProposal)
                .WithMany()
                .HasForeignKey(x => x.SourceProposalId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<WastewaterStream>(e =>
        {
            e.Property(x => x.StreamName).HasMaxLength(150);
            e.Property(x => x.FlowM3PerDay).HasColumnName("flow_m3_per_day").HasPrecision(12, 2);
            e.Property(x => x.CodMgL).HasPrecision(12, 2);
            e.Property(x => x.BodMgL).HasPrecision(12, 2);
            e.Property(x => x.TssMgL).HasPrecision(12, 2);
            e.Property(x => x.TdsMgL).HasPrecision(12, 2);
            e.Property(x => x.OilGreaseMgL).HasPrecision(12, 2);
            e.Property(x => x.Ph).HasPrecision(4, 2);
            e.Property(x => x.TemperatureC).HasPrecision(5, 2);
            e.HasOne(x => x.Project)
                .WithMany(p => p.Streams)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<Proposal>(e =>
        {
            e.HasIndex(x => new { x.ProposalNo, x.VersionNo }).IsUnique();
            e.Property(x => x.ProposalNo).HasMaxLength(40);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength();
            e.Property(x => x.TotalCost).HasPrecision(16, 2);
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasOne(x => x.Project)
                .WithMany(p => p.Proposals)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Enquiry)
                .WithMany(p => p.Proposals)
                .HasForeignKey(x => x.EnquiryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProposalLineItem>(e =>
        {
            e.ToTable("proposal_line_items");
            e.HasIndex(x => x.ProposalId);
            e.HasIndex(x => x.Category);
            e.Property(x => x.Category).HasConversion<byte>();
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(14, 3);
            e.Property(x => x.Rate).HasPrecision(14, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2).ValueGeneratedOnAddOrUpdate();
            e.HasOne(x => x.Proposal)
                .WithMany(p => p.LineItems)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProposalStatusHistory>(e =>
        {
            e.ToTable("proposal_status_history");
            e.HasIndex(x => x.ProposalId);
            e.Property(x => x.FromStatus).HasConversion<byte?>();
            e.Property(x => x.ToStatus).HasConversion<byte>();
            e.Property(x => x.ChangedBy).HasMaxLength(100);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.HasOne(x => x.Proposal)
                .WithMany(p => p.StatusHistory)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TreatmentTrain>(e =>
        {
            e.Property(x => x.TrainName).HasMaxLength(150);
            e.HasOne(x => x.Proposal)
                .WithMany(p => p.Trains)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UnitOperation>(e =>
        {
            e.HasIndex(x => new { x.TrainId, x.SequenceNo }).IsUnique();
            e.Property(x => x.UnitType).HasConversion<byte>();
            e.HasOne(x => x.Train)
                .WithMany(t => t.UnitOperations)
                .HasForeignKey(x => x.TrainId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EquipmentItem>(e =>
        {
            e.Property(x => x.TagNo).HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.Material).HasConversion<byte>();
            e.Property(x => x.ParametersJson).HasColumnType("json");
            e.HasOne(x => x.UnitOperation)
                .WithMany(u => u.EquipmentItems)
                .HasForeignKey(x => x.UnitOperationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoqLine>(e =>
        {
            e.HasIndex(x => x.Category);
            e.Property(x => x.Category).HasConversion<byte>();
            e.Property(x => x.TagNo).HasMaxLength(30);
            e.Property(x => x.ItemDescription).HasMaxLength(300);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.Quantity).HasPrecision(14, 3);
            e.Property(x => x.Rate).HasPrecision(14, 2);
            e.Property(x => x.Amount).HasPrecision(18, 2).ValueGeneratedOnAddOrUpdate();
            e.HasOne(x => x.Proposal)
                .WithMany(p => p.BoqLines)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.EquipmentItem)
                .WithMany(i => i.BoqLines)
                .HasForeignKey(x => x.EquipmentItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DrawingRecord>(e =>
        {
            e.Property(x => x.DrawingNo).HasMaxLength(50);
            e.Property(x => x.FileName).HasMaxLength(250);
            e.Property(x => x.FilePath).HasMaxLength(500);
            e.Property(x => x.DrawingType).HasConversion<byte>();
            e.HasOne(x => x.Proposal)
                .WithMany(p => p.Drawings)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DesignRule>(e =>
        {
            e.HasIndex(x => x.RuleCode).IsUnique();
            e.HasIndex(x => new { x.PlantType, x.Category });
            e.Property(x => x.PlantType).HasConversion<byte>();
            e.Property(x => x.Category).HasMaxLength(30);
            e.Property(x => x.RuleCode).HasMaxLength(80);
            e.Property(x => x.Parameter).HasMaxLength(80);
            e.Property(x => x.AppliesTo).HasMaxLength(100);
            e.Property(x => x.MinValue).HasPrecision(16, 4);
            e.Property(x => x.MaxValue).HasPrecision(16, 4);
            e.Property(x => x.Unit).HasMaxLength(30);
            e.Property(x => x.Authority).HasMaxLength(60);
            e.Property(x => x.SourceReference).HasMaxLength(300);
            e.Property(x => x.Remarks).HasMaxLength(500);
        });

        modelBuilder.Entity<RateCard>(e =>
        {
            e.HasIndex(x => new { x.ItemCode, x.EffectiveFrom }).IsUnique();
            e.Property(x => x.ItemCode).HasMaxLength(40);
            e.Property(x => x.Description).HasMaxLength(250);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.Rate).HasPrecision(14, 2);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsFixedLength();
            e.Property(x => x.EffectiveFrom).HasColumnType("date");
        });
    }

    public override int SaveChanges()
    {
        StampUpdatedAt();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampUpdatedAt();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampUpdatedAt()
    {
        var modified = ChangeTracker.Entries<BaseEntity>()
            .Where(x => x.State == EntityState.Modified);

        foreach (var entry in modified)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}