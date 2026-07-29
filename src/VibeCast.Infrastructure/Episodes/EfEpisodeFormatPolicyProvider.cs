using Microsoft.EntityFrameworkCore;
using VibeCast.Application.Episodes;
using VibeCast.Domain.Episodes;
using VibeCast.Infrastructure.Data;

namespace VibeCast.Infrastructure.Episodes;

public sealed class EfEpisodeFormatPolicyProvider(
    IDbContextFactory<VibeCastDbContext> dbContextFactory)
    : IEpisodeFormatPolicyProvider
{
    public async Task<EpisodeFormatGuidance> GetCurrentAsync(
        EpisodeFormatGuidanceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        string targetAudience =
            context.TargetAudience.Trim();

        string tone =
            context.Tone.Trim();

        await using VibeCastDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        List<EpisodeFormatPolicy> effectivePoliciesDb =
            await dbContext.EpisodeFormatPolicies
                .AsNoTracking()
                .Where(policy => policy.IsActive)
                .ToListAsync(cancellationToken);

        List<EpisodeFormatPolicy> effectivePolicies =
            effectivePoliciesDb
                .Where(policy =>
                    policy.EffectiveFromUtc <= context.PlanningTimeUtc)
                .Where(policy =>
                    policy.EffectiveToUtc == null ||
                    policy.EffectiveToUtc >= context.PlanningTimeUtc)
                .ToList();

        EpisodeFormatPolicy? selectedPolicy =
            effectivePolicies
                .Where(policy =>
                    MatchesTone(policy, tone))
                .Where(policy =>
                    MatchesAudience(
                        policy,
                        targetAudience))
                .OrderByDescending(
                    policy => CalculateSpecificity(
                        policy,
                        targetAudience,
                        tone))
                .ThenByDescending(
                    policy => policy.Priority)
                .ThenByDescending(
                    policy => policy.EffectiveFromUtc)
                .FirstOrDefault();

        if (selectedPolicy is null)
        {
            throw new InvalidOperationException(
                "No active episode-format policy matches the " +
                "current planning request, and no default policy " +
                "is available.");
        }

        return new EpisodeFormatGuidance(
            PolicyVersion: selectedPolicy.Version,
            TargetDurationMinutes:
                selectedPolicy.TargetDurationMinutes,
            PacingGuidance:
                selectedPolicy.PacingGuidance,
            Rationale:
                selectedPolicy.Rationale);
    }

    private static bool MatchesTone(
        EpisodeFormatPolicy policy,
        string tone)
    {
        return policy.Tone is null ||
               policy.Tone.Equals(
                   tone,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAudience(
        EpisodeFormatPolicy policy,
        string targetAudience)
    {
        return policy.AudienceKeyword is null ||
               targetAudience.Contains(
                   policy.AudienceKeyword,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static int CalculateSpecificity(
        EpisodeFormatPolicy policy,
        string targetAudience,
        string tone)
    {
        int score = 0;

        if (policy.Tone is not null &&
            policy.Tone.Equals(
                tone,
                StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }

        if (policy.AudienceKeyword is not null &&
            targetAudience.Contains(
                policy.AudienceKeyword,
                StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        return score;
    }
}
