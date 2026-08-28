using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VibeCast.Application.Media;
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
    public DbSet<EpisodeFormatPolicy> EpisodeFormatPolicies => Set<EpisodeFormatPolicy>();

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

            entity.Property(episode => episode.TargetAudience)
            .HasMaxLength(160)
            .IsRequired();

            entity.Property(episode => episode.Objective)
                .HasMaxLength(600)
                .IsRequired();

            entity.Property(episode => episode.Tone)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(episode => episode.Language)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(episode => episode.PlannedPublishDate);

            entity.Property(episode => episode.AcceptedPlanJson);

            entity.Property(episode => episode.PlanPromptVersion)
                .HasMaxLength(120);

            entity.Property(episode => episode.PlanGeneratedAtUtc);

            entity.Property(episode => episode.PlanRepairAttempted);

            entity.Property(episode => episode.PlanRepairPromptVersion)
                .HasMaxLength(120);

            entity.Property(episode => episode.PlanFormatPolicyVersion)
                .HasMaxLength(120);

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

            entity.Property(x => x.ProposedAltText).HasMaxLength(ArtworkAnalysisValidator.MaximumAltTextLength);

            entity.Property(x => x.AcceptedAltText)
                .HasMaxLength(
                    ArtworkAnalysisValidator
                        .MaximumAltTextLength);

            entity.Property(x => x.ArtworkSummary)
                .HasMaxLength(
                    ArtworkAnalysisValidator
                        .MaximumSummaryLength);

            entity.Property(x => x.ArtworkVisibleText)
                .HasMaxLength(
                    ArtworkAnalysisValidator
                        .MaximumVisibleTextLength);

            entity.Property(x => x.ArtworkPromptVersion)
                .HasMaxLength(80);

            entity.Property(x => x.GenerationModelDeployment)
                .HasMaxLength(120);

            entity.Property(x => x.GenerationPromptVersion)
                .HasMaxLength(80);

            entity.Property(x => x.TranscriptText);

            entity.Property(x => x.TranscriptionLocale)
                .HasMaxLength(20);

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

        builder.Entity<EpisodeFormatPolicy>(entity =>
        {
            entity.ToTable("EpisodeFormatPolicies");

            entity.HasKey(policy => policy.Id);

            entity.Property(policy => policy.Version)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(policy => policy.Tone)
                .HasMaxLength(80);

            entity.Property(policy => policy.AudienceKeyword)
                .HasMaxLength(80);

            entity.Property(policy => policy.PacingGuidance)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(policy => policy.Rationale)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(policy => policy.Version)
                .IsUnique();

            entity.HasIndex(
                policy => new
                {
                    policy.IsActive,
                    policy.EffectiveFromUtc
                });
        });
    }
}
