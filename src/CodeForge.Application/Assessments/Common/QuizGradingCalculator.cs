namespace CodeForge.Application.Assessments.Common
{
    public static class QuizGradingCalculator
    {
        public record Result(int Score, bool? Passed);

        public static Result Calculate(int totalQuestions, int correctCount, int? passScore)
        {
            var score = totalQuestions == 0 ? 0 : (int)Math.Round((decimal)correctCount / totalQuestions * 100m);
            bool? passed = passScore.HasValue ? score >= passScore.Value : null;
            return new Result(score, passed);
        }
    }
}
