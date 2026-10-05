using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class SupportingSourceAssessmentValidatorTests
{
    [TestMethod]
    public void Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid()
    {
        // PRACTICE S08T-03: Construct an assessment containing an evidence requirement outside the accepted plan. Assert the validator reports the relevant field failure.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S08T-03: implement Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid.");
    }
}
