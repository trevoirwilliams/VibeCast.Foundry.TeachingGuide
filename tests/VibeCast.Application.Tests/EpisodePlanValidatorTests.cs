using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class EpisodePlanValidatorTests
{
    [TestMethod]
    public void Validate_ValidPlan_IsValid()
    {
        EpisodePlan plan = CreateValidPlan();

        var result = new EpisodePlanValidator().Validate(plan);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public void Validate_SegmentDurationsThatDontSumToTarget_IsInvalid()
    {
        // Three segments x 5 min = 15, but TargetDurationMinutes = 20.
        EpisodePlan plan = new(
            Summary: "A practical episode summary.",
            TargetDurationMinutes: 20,
            Segments:
            [
                new(Sequence: 1, Title: "Opening",  Purpose: "Set context.",      DurationMinutes: 5, TalkingPoints: ["Point A", "Point B"]),
                new(Sequence: 2, Title: "Core",     Purpose: "Main discussion.",   DurationMinutes: 5, TalkingPoints: ["Point A", "Point B"]),
                new(Sequence: 3, Title: "Close",    Purpose: "Wrap up.",           DurationMinutes: 5, TalkingPoints: ["Point A", "Point B"]),
            ],
            KeyMessages: ["Message one.", "Message two."],
            EvidenceRequirements: [],
            MediaRequirements: [],
            EditorialRisks: []);

        var result = new EpisodePlanValidator().Validate(plan);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(
            result.Errors.Any(e => e.PropertyName == "Segments.TotalDuration"),
            "Expected a Segments.TotalDuration error.");
    }

    private static EpisodePlan CreateValidPlan() =>
        new(
            Summary: "A practical summary for this episode.",
            TargetDurationMinutes: 20,
            Segments:
            [
                new(Sequence: 1, Title: "Opening", Purpose: "Set context.",      DurationMinutes: 7, TalkingPoints: ["Point A", "Point B"]),
                new(Sequence: 2, Title: "Core",    Purpose: "Main discussion.",   DurationMinutes: 7, TalkingPoints: ["Point A", "Point B"]),
                new(Sequence: 3, Title: "Close",   Purpose: "Wrap up.",           DurationMinutes: 6, TalkingPoints: ["Point A", "Point B"]),
            ],
            KeyMessages: ["Message one.", "Message two."],
            EvidenceRequirements: [],
            MediaRequirements: [],
            EditorialRisks: []);
}
