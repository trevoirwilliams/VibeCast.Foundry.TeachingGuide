using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Domain.Episodes;

namespace VibeCast.Domain.Tests;

[TestClass]
public sealed class EpisodeTests
{
    [TestMethod]
        public void Create_WithValidValues_CreatesDraft()
        {
            var episode = Episode.Create(
                title: "AI-ready architecture",
                description: "Description",
                targetAudience: "Developers",
                objective: "Validate episode creation",
                tone: "Professional",
                language: "English",
                plannedPublishDate: null,
                ownerId: "user-1");

            Assert.AreEqual(EpisodeStatus.Draft, episode.Status);
            Assert.AreEqual("AI-ready architecture", episode.Title);
            Assert.AreEqual("user-1", episode.OwnerId);
        }

    [TestMethod]
        public void Create_WithBlankTitle_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Episode.Create(
                title: " ",
                description: null,
                targetAudience: "Developers",
                objective: "Test",
                tone: "Neutral",
                language: "English",
                plannedPublishDate: null,
                ownerId: "user-1"));
        }

    [TestMethod]
    public void SaveAcceptedPlan_WithBlankPlanJson_Throws()
    {
        Episode episode = Episode.Create(
            title: "Test Episode",
            description: null,
            targetAudience: "Developers",
            objective: "Test the blank-json guard",
            tone: "Neutral",
            language: "English",
            plannedPublishDate: null,
            ownerId: "owner-1");

        Assert.ThrowsExactly<ArgumentException>(() =>
            episode.SaveAcceptedPlan(
                planJson: " ",
                promptVersion: "v1",
                generatedAtUtc: DateTimeOffset.UtcNow,
                repairAttempted: false,
                repairPromptVersion: null,
                formatPolicyVersion: null));

        Assert.IsNull(episode.AcceptedPlanJson);
    }
}
