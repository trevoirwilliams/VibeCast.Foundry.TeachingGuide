using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VibeCast.Domain.Episodes;
using VibeCast.Domain.Jobs;
using VibeCast.Domain.Media;
using VibeCast.Domain.Users;

namespace VibeCast.Infrastructure.Data;

public sealed class VibeCastDbContext(DbContextOptions<VibeCastDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Episode>(entity =>
        {
            entity.ToTable("Episodes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2_000);
            entity.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => new { x.OwnerId, x.CreatedAtUtc });
        });

        builder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("MediaAssets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.StorageKey).IsUnique();
        });

        builder.Entity<ProcessingJob>(entity =>
        {
            entity.ToTable("ProcessingJobs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.JobType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SubjectReference).HasMaxLength(512);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2_000);
            entity.HasIndex(x => new { x.OwnerId, x.CreatedAtUtc });
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IdentityUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.IdentityUserId).IsUnique();
        });
    }
}
