using SummitLog.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Peak> Peaks => Set<Peak>();
    public DbSet<VisitedPeak> VisitedPeaks => Set<VisitedPeak>();
    public DbSet<BucketListEntry> BucketListEntries => Set<BucketListEntry>();
    public DbSet<ElevationOverride> ElevationOverrides => Set<ElevationOverride>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Peak>()
            .HasIndex(p => p.GeoNameId)
            .IsUnique();

        builder.Entity<Peak>()
            .HasIndex(p => p.Name);

        builder.Entity<VisitedPeak>()
            .HasIndex(v => new { v.UserId, v.PeakId })
            .IsUnique();

        builder.Entity<VisitedPeak>()
            .HasOne(v => v.Peak)
            .WithMany(p => p.VisitedByUsers)
            .HasForeignKey(v => v.PeakId);

        builder.Entity<VisitedPeak>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId);

        builder.Entity<BucketListEntry>()
            .HasIndex(b => new { b.UserId, b.PeakId })
            .IsUnique();

        builder.Entity<BucketListEntry>()
            .HasOne(b => b.Peak)
            .WithMany(p => p.BucketListedByUsers)
            .HasForeignKey(b => b.PeakId);

        builder.Entity<BucketListEntry>()
            .HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId);

        builder.Entity<ElevationOverride>()
            .HasIndex(e => e.PeakId)
            .IsUnique();

        builder.Entity<ElevationOverride>()
            .HasOne(e => e.Peak)
            .WithOne(p => p.ElevationOverride)
            .HasForeignKey<ElevationOverride>(e => e.PeakId);
    }
}
