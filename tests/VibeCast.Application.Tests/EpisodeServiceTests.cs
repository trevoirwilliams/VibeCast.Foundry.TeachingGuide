using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Domain.Episodes;
using VibeCast.Domain.Media;
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

        DbContextOptions<VibeCastDbContext> options = new DbContextOptionsBuilder<VibeCastDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid recentlyUpdatedEpisodeId;

        await using (var db = new VibeCastDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            Episode recentlyUpdated = CreateEpisode("Older owner episode", "owner-1");
            Episode secondOwnerEpisode = CreateEpisode("Second owner episode", "owner-1");
            Episode otherOwnerEpisode = CreateEpisode("Other owner episode", "owner-2");

            db.Episodes.AddRange(
                recentlyUpdated,
                secondOwnerEpisode,
                otherOwnerEpisode);

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

    [TestMethod]
    public async Task GetAsync_ReturnsRelatedMediaAssetsForTheEpisode()
    {
        await using SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<VibeCastDbContext> options = new DbContextOptionsBuilder<VibeCastDbContext>()
            .UseSqlite(connection)
            .Options;

        Guid episodeId;

        await using (var db = new VibeCastDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            Episode episode = Episode.Create(
                title: "Episode with media",
                description: "Includes related assets.",
                targetAudience: "Developers",
                objective: "Verify media asset projection",
                tone: "Professional",
                language: "English",
                plannedPublishDate: null,
                ownerId: "owner-1");

            episodeId = episode.Id;

            db.Episodes.Add(episode);
            await db.SaveChangesAsync();

            MediaAsset firstAsset = MediaAsset.Create(
                episodeId: episode.Id,
                ownerId: "owner-1",
                originalFileName: "notes.pdf",
                storageKey: "media/notes.pdf",
                contentType: "application/pdf",
                sizeBytes: 1024);

            firstAsset.MarkValidated();

            MediaAsset secondAsset = MediaAsset.Create(
                episodeId: episode.Id,
                ownerId: "owner-1",
                originalFileName: "cover.png",
                storageKey: "media/cover.png",
                contentType: "image/png",
                sizeBytes: 2048);

            secondAsset.MarkValidated();

            db.MediaAssets.AddRange(firstAsset, secondAsset);
            await db.SaveChangesAsync();
        }

        var service = new EfEpisodeService(new TestDbContextFactory(options));

        EpisodeDetails? details = await service.GetAsync(
            episodeId,
            ownerId: "owner-1");

        Assert.IsNotNull(details);
        Assert.AreEqual(2, details.MediaAssets.Count);
        Assert.AreEqual("cover.png", details.MediaAssets[0].OriginalFileName);
        Assert.AreEqual("notes.pdf", details.MediaAssets[1].OriginalFileName);
        Assert.AreEqual("image/png", details.MediaAssets[0].ContentType);
        Assert.AreEqual("application/pdf", details.MediaAssets[1].ContentType);
    }

    private sealed class TestDbContextFactory(DbContextOptions<VibeCastDbContext> options) : IDbContextFactory<VibeCastDbContext>
    {
        public VibeCastDbContext CreateDbContext() => new(options);

        public ValueTask<VibeCastDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(CreateDbContext());
    }

    private static Episode CreateEpisode(string title, string ownerId) =>
        Episode.Create(
            title: title,
            description: $"Description for {title}.",
            targetAudience: "Developers",
            objective: "Verify owner-scoped episode queries",
            tone: "Professional",
            language: "English",
            plannedPublishDate: null,
            ownerId: ownerId);
}
