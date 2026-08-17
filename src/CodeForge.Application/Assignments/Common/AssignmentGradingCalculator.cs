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

        // Compute, don't store: Assignment/AssignmentSubmission carry no Passed column.
        // Null whenever there's nothing to compare — not graded yet, or the assignment
        // has no pass threshold — never a fake pass/fail (mirrors NextCohortSelector's
        // null discipline).
        public static bool? ComputePassed(int? finalScore, int? passScore)
        {
            if (finalScore is null || passScore is null)
            {
                return null;
            }

            return finalScore >= passScore;
        }
    }
}
