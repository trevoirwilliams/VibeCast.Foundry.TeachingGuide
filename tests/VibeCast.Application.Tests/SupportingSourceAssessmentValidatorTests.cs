using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class SupportingSourceAssessmentValidatorTests
{
    [TestMethod]
    public void Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid()
    {
        SupportingSourceAssessment assessment = new(
            IsRelevant: true,
            Rationale:
                "The document discusses adoption and migration challenges.",
            Summary:
                "A source covering enterprise migration experience.",
            RelevantPoints:
            [
                "Organizations reported migration challenges."
            ],
            MatchedEvidenceRequirements:
            [
                "Current adoption statistics",
                "Gartner market forecast"
            ]);

        SupportingSourceAssessmentValidationRequest request =
            new(
                assessment,
                [
                    "Current adoption statistics",
                    "Documented migration challenges"
                ]);

        var result =
            new SupportingSourceAssessmentValidator()
                .Validate(request);

        Assert.IsFalse(result.IsValid);

        Assert.IsTrue(
            result.Errors.Any(
                error =>
                    error.PropertyName ==
                    nameof(
                        SupportingSourceAssessment
                            .MatchedEvidenceRequirements)));
    }
}
