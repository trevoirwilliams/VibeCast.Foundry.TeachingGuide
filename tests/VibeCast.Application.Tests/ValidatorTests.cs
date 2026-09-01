using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class ValidatorTests
{
    [TestMethod]
    public void EpisodeValidator_RejectsPastSchedule()
    {
        var request = new CreateEpisodeRequest
        {
            Title = "Valid title",
            ScheduledForUtc = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var result = new EpisodeDraftValidator().Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(error => error.PropertyName == nameof(request.ScheduledForUtc)));
    }

    [TestMethod]
    public void MediaValidator_RejectsUnsupportedExtension()
    {
        var request = new MediaUploadRequest("payload.exe", "application/octet-stream", 100, null);

        var result = new MediaUploadValidator().Validate(request);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void MediaValidator_AllowsPdfAndTextInTheSharedSourceGallery()
    {
        var validator = new MediaUploadValidator();

        var pdfResult = validator.Validate(
            new MediaUploadRequest(
                "research.pdf",
                "application/pdf",
                100,
                EpisodeId: null));

        var textResult = validator.Validate(
            new MediaUploadRequest(
                "notes.TXT",
                "text/plain",
                100,
                EpisodeId: null));

        Assert.IsTrue(pdfResult.IsValid);
        Assert.IsTrue(textResult.IsValid);
    }

    [TestMethod]
    public void MediaValidator_RestrictsOnlySharedUploadsToPdfAndText()
    {
        var validator = new MediaUploadValidator();

        var sharedResult = validator.Validate(
            new MediaUploadRequest(
                "cover.png",
                "image/png",
                100,
                EpisodeId: null));

        var episodeResult = validator.Validate(
            new MediaUploadRequest(
                "cover.png",
                "image/png",
                100,
                EpisodeId: Guid.NewGuid()));

        Assert.IsFalse(sharedResult.IsValid);
        Assert.IsTrue(episodeResult.IsValid);
    }
}
