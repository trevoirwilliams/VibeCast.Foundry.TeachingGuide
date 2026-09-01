using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Domain.Episodes;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Episodes;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class EpisodeServiceTests
{
    [TestMethod]
    public async Task ListAsync_ReturnsOnlyOwnerEpisodesWithMostRecentlyUpdatedFirst()
    {
        await using SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<VibeCastDbContext> options =
            new DbContextOptionsBuilder<VibeCastDbContext>()
                .UseSqlite(connection)
                .Options;

        Guid recentlyUpdatedEpisodeId;

        await using (var db = new VibeCastDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            Episode recentlyUpdated = CreateEpisode("Older owner episode", "owner-1");
            Episode secondOwnerEpisode = CreateEpisode("Second owner episode", "owner-1");
            Episode otherOwnerEpisode = CreateEpisode("Other owner episode", "owner-2");

            db.Episodes.AddRange(recentlyUpdated, secondOwnerEpisode, otherOwnerEpisode);
            await db.SaveChangesAsync();

            recentlyUpdated.Rename("Most recently updated episode");
            recentlyUpdatedEpisodeId = recentlyUpdated.Id;
            await db.SaveChangesAsync();
        }

        var service = new EfEpisodeService(new TestDbContextFactory(options));

        IReadOnlyList<EpisodeSummary> episodes =
            await service.ListAsync("owner-1");

        Assert.AreEqual(2, episodes.Count);
        Assert.AreEqual(recentlyUpdatedEpisodeId, episodes[0].Id);
        Assert.AreEqual("Most recently updated episode", episodes[0].Title);
        Assert.IsFalse(episodes.Any(episode => episode.Title == "Other owner episode"));
    }

    private static Episode CreateEpisode(string title, string ownerId) =>
        Episode.Create(
            title,
            $"Description for {title}.",
            "Developers",
            "Verify owner-scoped episode queries",
            "Professional",
            "English",
            plannedPublishDate: null,
            ownerId: ownerId);

    private sealed class TestDbContextFactory(
        DbContextOptions<VibeCastDbContext> options)
        : IDbContextFactory<VibeCastDbContext>
    {
        public VibeCastDbContext CreateDbContext() => new(options);

        public ValueTask<VibeCastDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }
}
