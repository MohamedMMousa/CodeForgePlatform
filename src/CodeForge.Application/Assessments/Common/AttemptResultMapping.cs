using CodeForge.Domain.Entities;

namespace CodeForge.Application.Assessments.Common
{
    public static class AttemptResultMapping
    {
        public static AttemptResultDto ToDto(QuizAttempt attempt)
        {
            var answers = attempt.Quiz.Questions.OrderBy(q => q.OrderIndex).Select(question =>
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                var options = question.Options.OrderBy(o => o.OrderIndex).Select(o => new OptionDto(o.Id, o.OptionText, o.IsCorrect)).ToList();
                bool? isCorrectSelection = answer?.SelectedOptionId is null
                    ? null
                    : question.Options.Any(o => o.Id == answer.SelectedOptionId && o.IsCorrect);

                return new AnswerResultDto(question.Id, question.QuestionText, answer?.SelectedOptionId, isCorrectSelection, options);
            }).ToList();

            return new AttemptResultDto(
                attempt.Id,
                attempt.QuizId,
                attempt.Quiz.Title,
                attempt.AttemptNumber,
                attempt.Score,
                attempt.Passed,
                attempt.StartedAt,
                attempt.SubmittedAt,
                answers);
        }
    }
}
