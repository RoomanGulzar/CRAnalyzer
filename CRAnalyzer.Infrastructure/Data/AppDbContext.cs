using CRAnalyzer.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CRAnalyzer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AffectedFile> AffectedFiles => Set<AffectedFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CRDocumentPath).HasMaxLength(512);
            entity.Property(e => e.CRDocumentName).HasMaxLength(256);
            entity.Property(e => e.ProjectPath).HasMaxLength(512);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasMany(e => e.AffectedFiles)
                  .WithOne(f => f.Analysis)
                  .HasForeignKey(f => f.AnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AffectedFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Reason).HasMaxLength(2000);
            entity.Property(e => e.SuggestedSteps).HasMaxLength(4000);
            entity.Property(e => e.ChangeType).HasConversion<string>();
        });
    }
}
