using CodeForge.Domain.Entities;

namespace CodeForge.Application.Assignments.Common
{
    public static class SubmissionResultMapping
    {
        public static SubmissionResultDto ToDto(AssignmentSubmission submission)
        {
            var results = submission.TestResults.Select(r =>
            {
                if (r.TestCase.IsHidden)
                {
                    return new TestResultDto(r.TestCaseId, true, r.Passed, null, null, null);
                }

                return new TestResultDto(r.TestCaseId, false, r.Passed, r.ActualOutput, r.ErrorMessage, r.ExecutionTimeMs);
            }).ToList();

            return new SubmissionResultDto(
                submission.Id,
                submission.AttemptNumber,
                submission.SubmittedAt,
                submission.IsLate,
                submission.AutoScore,
                submission.AutoGradingStatus,
                submission.ManualScore,
                submission.ManualFeedback,
                submission.FinalScore,
                results);
        }
    }
}
