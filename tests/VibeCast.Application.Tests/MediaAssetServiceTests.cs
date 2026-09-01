using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Media;
using VibeCast.Domain.Episodes;
using VibeCast.Domain.Media;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Media;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class MediaAssetServiceTests
{
    [TestMethod]
    public async Task ListSharedSourcesAsync_ReturnsOnlyOwnerPdfAndTextAssets()
    {
        await using SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();

        DbContextOptions<VibeCastDbContext> options =
            new DbContextOptionsBuilder<VibeCastDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var db = new VibeCastDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();

            Episode episode = Episode.Create(
                "Episode with an attached source",
                "Testing source ownership.",
                "Developers",
                "Verify source gallery filtering",
                "Professional",
                "English",
                plannedPublishDate: null,
                ownerId: "owner-1");

            db.Episodes.Add(episode);

            db.MediaAssets.AddRange(
                CreateAsset(null, "owner-1", "research.pdf", "application/pdf"),
                CreateAsset(null, "owner-1", "notes.txt", "text/plain"),
                CreateAsset(null, "owner-1", "legacy-cover.png", "image/png"),
                CreateAsset(episode.Id, "owner-1", "episode.pdf", "application/pdf"),
                CreateAsset(null, "owner-2", "private.pdf", "application/pdf"));

            await db.SaveChangesAsync();
        }

        var service = new EfMediaAssetService(
            new TestDbContextFactory(options),
            blobStorage: null!,
            new MediaUploadValidator(),
            artworkValidator: null!,
            logger: null!);

        IReadOnlyList<MediaAssetSummary> sources =
            await service.ListSharedSourcesAsync("owner-1");

        Assert.AreEqual(2, sources.Count);
        CollectionAssert.AreEquivalent(
            new[] { "research.pdf", "notes.txt" },
            sources.Select(source => source.OriginalFileName).ToArray());
        Assert.IsTrue(sources.All(source => source.EpisodeId is null));
    }

    private static MediaAsset CreateAsset(
        Guid? episodeId,
        string ownerId,
        string fileName,
        string contentType)
    {
        MediaAsset asset = MediaAsset.Create(
            episodeId,
            ownerId,
            fileName,
            $"media/{Guid.NewGuid():N}/{fileName}",
            contentType,
            sizeBytes: 100);

        asset.MarkValidated();
        return asset;
    }

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
