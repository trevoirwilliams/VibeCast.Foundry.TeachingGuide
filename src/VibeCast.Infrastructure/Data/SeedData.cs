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
                Episode.Create("Modern .NET AI Architecture", "Seed episode used by the Section 04 starter branch.", user.Id),
                Episode.Create("Responsible Multimodal Workflows", "A second seeded record for the dashboard and list screens.", user.Id));
        }

        if (!await db.ProcessingJobs.AnyAsync(cancellationToken))
        {
            var completed = ProcessingJob.Queue(user.Id, "Starter data initialization", "seed");
            completed.Start();
            completed.Complete();
            db.ProcessingJobs.Add(completed);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
