using Microsoft.VisualStudio.TestTools.UnitTesting;
using VibeCast.Application.Episodes;

namespace VibeCast.Application.Tests;

[TestClass]
public sealed class SupportingSourceAssessmentValidatorTests
{
    [TestMethod]
    public void Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid()
    {
        // PRACTICE S08T-03: Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid
        // 1. Create an otherwise-valid assessment with one unknown evidence requirement.
        // 2. Pass it with the plan's allowed requirements to the real validator.
        // 3. Assert invalidity and an error for the matched-evidence field.
        // 4. Ensure unrelated invalid fields cannot explain the test passing.
        // Optional API hints and checks: docs/practice/README.md#s08t-03-validate_whenassessmentreferencesunknownevidencerequirement_isinvalid
        throw new NotImplementedException("S08T-03: implement Validate_WhenAssessmentReferencesUnknownEvidenceRequirement_IsInvalid.");
    }
}
