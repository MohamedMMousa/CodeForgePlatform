using CodeForge.Domain.Entities;

namespace CodeForge.Application.Assessments.Common
{
    public static class AssessmentMapping
    {
        public static AssessmentDto ToDto(Quiz quiz)
        {
            return new AssessmentDto(
                quiz.Id,
                quiz.ModuleId,
                quiz.Type,
                quiz.Title,
                quiz.OrderIndex,
                quiz.TimeLimitMinutes,
                quiz.PassScore,
                quiz.IsPractice,
                quiz.MaxAttempts,
                quiz.RandomizeQuestions,
                quiz.DisableCopyPaste,
                quiz.Questions.Count,
                quiz.CreatedAt,
                quiz.UpdatedAt);
        }

        public static AssessmentDetailDto ToDetailDto(Quiz quiz)
        {
            return new AssessmentDetailDto(
                quiz.Id,
                quiz.ModuleId,
                quiz.Type,
                quiz.Title,
                quiz.OrderIndex,
                quiz.TimeLimitMinutes,
                quiz.PassScore,
                quiz.IsPractice,
                quiz.MaxAttempts,
                quiz.RandomizeQuestions,
                quiz.DisableCopyPaste,
                quiz.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuestionDto(
                        q.Id,
                        q.QuestionText,
                        q.OrderIndex,
                        q.Options.OrderBy(o => o.OrderIndex).Select(o => new OptionDto(o.Id, o.OptionText, o.IsCorrect)).ToList()))
                    .ToList());
        }
    }
}
