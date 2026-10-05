using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Domain.Episodes;

namespace VibeCast.Domain.Tests;

[TestClass]
public sealed class EpisodeTests
{
    [TestMethod]
    public void Create_WithValidValues_CreatesDraft()
    {
        var episode = Episode.Create("AI-ready architecture", "Description", "user-1");

        Assert.AreEqual(EpisodeStatus.Draft, episode.Status);
        Assert.AreEqual("AI-ready architecture", episode.Title);
        Assert.AreEqual("user-1", episode.OwnerId);
    }

    [TestMethod]
    public void Create_WithBlankTitle_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Episode.Create(" ", null, "user-1"));
    }
}
