namespace CodeForge.Application.Assignments.Common
{
    public static class AssignmentGradingCalculator
    {
        public record TestCaseOutcome(int Points, bool Passed);

        public static int CalculateAutoScore(IReadOnlyList<TestCaseOutcome> outcomes)
        {
            var totalPoints = outcomes.Sum(o => o.Points);
            if (totalPoints == 0)
            {
                return 0;
            }

            var earnedPoints = outcomes.Where(o => o.Passed).Sum(o => o.Points);
            return (int)Math.Round((decimal)earnedPoints / totalPoints * 100m);
        }
    }
}
