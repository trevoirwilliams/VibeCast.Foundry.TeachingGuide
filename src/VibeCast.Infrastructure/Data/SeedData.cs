using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VibeCast.Domain.Episodes;
using VibeCast.Domain.Jobs;
using VibeCast.Domain.Users;

namespace VibeCast.Infrastructure.Data;

public static class SeedData
{
    public const string DevelopmentEmail = "instructor@vibecast.local";
    public const string DevelopmentPassword = "VibeCast!12345";

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<VibeCastDbContext>>();

        var user = await userManager.FindByEmailAsync(DevelopmentEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = DevelopmentEmail,
                Email = DevelopmentEmail,
                EmailConfirmed = true,
                DisplayName = "VibeCast Instructor"
            };

            var result = await userManager.CreateAsync(user, DevelopmentPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.UserProfiles.AnyAsync(cancellationToken))
        {
            db.UserProfiles.Add(UserProfile.Create(user.Id, user.DisplayName));
        }

        if (!await db.Episodes.AnyAsync(cancellationToken))
        {
            db.Episodes.AddRange(
                Episode.Create(
                    title: "Modern .NET AI Architecture",
                    description: "Seed episode used by the Section 04 starter branch.",
                    targetAudience: "Developers",
                    objective: "Seed data",
                    tone: "Informative",
                    language: "English",
                    plannedPublishDate: null,
                    ownerId: user.Id),
                Episode.Create(
                    title: "Responsible Multimodal Workflows",
                    description: "A second seeded record for the dashboard and list screens.",
                    targetAudience: "Developers",
                    objective: "Seed data",
                    tone: "Informative",
                    language: "English",
                    plannedPublishDate: null,
                    ownerId: user.Id));
        }

        if (!await db.ProcessingJobs.AnyAsync(cancellationToken))
        {
            var completed = ProcessingJob.Queue(user.Id, "Starter data initialization", "seed");
            completed.Start();
            completed.Complete();
            db.ProcessingJobs.Add(completed);
        }

        if (!await db.EpisodeFormatPolicies.AnyAsync(cancellationToken))
        {
            DateTimeOffset effectiveFromUtc =
                new(
                    year: 2026,
                    month: 1,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero);

            db.EpisodeFormatPolicies.AddRange(
                EpisodeFormatPolicy.Create(
                    version: "format-default-2026.1",
                    tone: null,
                    audienceKeyword: null,
                    targetDurationMinutes: 24,
                    pacingGuidance:
                        "Use balanced pacing with clear transitions, " +
                        "practical explanations, and a concise recap.",
                    rationale:
                        "Default guidance applies when no more specific " +
                        "audience or tone policy matches.",
                    priority: 0,
                    effectiveFromUtc: effectiveFromUtc),

                EpisodeFormatPolicy.Create(
                    version: "format-executive-2026.1",
                    tone: "Executive briefing",
                    audienceKeyword: null,
                    targetDurationMinutes: 18,
                    pacingGuidance:
                        "Lead with the decision, summarize the business " +
                        "impact, and minimize background explanation.",
                    rationale:
                        "Executive briefings prioritize decisions, impact, " +
                        "risk, and required action.",
                    priority: 100,
                    effectiveFromUtc: effectiveFromUtc),

                EpisodeFormatPolicy.Create(
                    version: "format-conversational-2026.1",
                    tone: "Conversational",
                    audienceKeyword: null,
                    targetDurationMinutes: 22,
                    pacingGuidance:
                        "Use short sections, natural transitions, concrete " +
                        "examples, and space for reflective commentary.",
                    rationale:
                        "Conversational delivery benefits from moderate " +
                        "length and less densely packed segments.",
                    priority: 100,
                    effectiveFromUtc: effectiveFromUtc),

                EpisodeFormatPolicy.Create(
                    version: "format-technical-2026.1",
                    tone: "Technical deep dive",
                    audienceKeyword: null,
                    targetDurationMinutes: 30,
                    pacingGuidance:
                        "Explain underlying mechanics, implementation " +
                        "trade-offs, failure modes, and production evidence.",
                    rationale:
                        "A technical deep dive requires sufficient time for " +
                        "mechanics, examples, and engineering trade-offs.",
                    priority: 100,
                    effectiveFromUtc: effectiveFromUtc),

                EpisodeFormatPolicy.Create(
                    version: "format-beginner-2026.1",
                    tone: null,
                    audienceKeyword: "beginner",
                    targetDurationMinutes: 26,
                    pacingGuidance:
                        "Introduce one idea at a time, explain it in plain " +
                        "language, demonstrate it, and recap before moving on.",
                    rationale:
                        "Beginner audiences require additional explanation " +
                        "and reinforcement.",
                    priority: 80,
                    effectiveFromUtc: effectiveFromUtc));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
