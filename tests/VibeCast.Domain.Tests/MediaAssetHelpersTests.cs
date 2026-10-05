using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Domain.Media;

namespace VibeCast.Domain.Tests;

[TestClass]
public sealed class MediaAssetHelpersTests
{
    [TestMethod]
    public void ContentTypeHelpers_ReturnExpectedMediaMetadata()
    {
        Assert.IsTrue(MediaAssetHelpers.IsSupportedArtwork("image/png"));
        Assert.IsFalse(MediaAssetHelpers.IsSupportedArtwork("application/pdf"));

        Assert.AreEqual("IM", MediaAssetHelpers.GetTypeCode("image/jpeg"));
        Assert.AreEqual("preview-image", MediaAssetHelpers.GetPreviewClass("image/png"));
        Assert.AreEqual("DOC", MediaAssetHelpers.GetTypeCode("application/zip"));
        Assert.AreEqual("Artwork or cover image ready for review.", MediaAssetHelpers.GetMediaSummary("image/png"));
        Assert.AreEqual("1.5 MB", MediaAssetHelpers.FormatSize(1_572_864));
    }
}
