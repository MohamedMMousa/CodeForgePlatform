using CodeForge.Application.Common.Constants;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Gradebook.Common
{
    public static class GradebookCalculator
    {
        public static List<AssessmentGradeDto> BuildAssessmentGrades(
            Guid studentId, IReadOnlyList<Quiz> quizzes, IReadOnlyList<QuizAttempt> attempts)
        {
            return quizzes.Select(quiz =>
            {
                var studentAttempts = attempts.Where(a => a.QuizId == quiz.Id && a.StudentId == studentId).ToList();
                var submitted = studentAttempts.Where(a => a.SubmittedAt != null).ToList();
                int? bestScore = submitted.Count == 0 ? null : submitted.Max(a => a.Score);
                bool? passed = submitted.Count == 0 ? null : submitted.Any(a => a.Passed == true);
                return new AssessmentGradeDto(quiz.Id, quiz.Title, quiz.Type, bestScore, passed, studentAttempts.Count);
            }).ToList();
        }

        public static List<AssignmentGradeDto> BuildAssignmentGrades(
            Guid studentId, IReadOnlyList<Assignment> assignments, IReadOnlyList<AssignmentSubmission> submissions)
        {
            return assignments.Select(assignment =>
            {
                var studentSubmissions = submissions.Where(s => s.AssignmentId == assignment.Id && s.StudentId == studentId).ToList();
                int? finalScore = studentSubmissions.Count == 0 ? null : studentSubmissions.Max(s => s.FinalScore);
                var latest = studentSubmissions.OrderByDescending(s => s.AttemptNumber).FirstOrDefault();
                var autoGradingStatus = latest?.AutoGradingStatus ?? AssignmentGradingStatuses.Pending;
                var manuallyGraded = studentSubmissions.Any(s => s.GradedAt != null);
                return new AssignmentGradeDto(assignment.Id, assignment.Title, finalScore, autoGradingStatus, manuallyGraded);
            }).ToList();
        }
    }
}
