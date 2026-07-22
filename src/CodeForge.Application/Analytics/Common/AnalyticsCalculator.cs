namespace CodeForge.Application.Analytics.Common
{
    /// <summary>
    /// Small pure helpers for dashboard rollups, kept separate so the arithmetic is unit
    /// tested independently of the EF queries that feed it.
    /// </summary>
    public static class AnalyticsCalculator
    {
        /// <summary>
        /// Pass rate as a 0–100 percentage (1 decimal). Denominator is the number of
        /// graded/submitted attempts; returns 0 when nothing has been submitted.
        /// </summary>
        public static decimal PassRate(int submittedCount, int passedCount)
        {
            if (submittedCount <= 0)
            {
                return 0m;
            }

            return Math.Round((decimal)passedCount / submittedCount * 100m, 1);
        }
    }
}
