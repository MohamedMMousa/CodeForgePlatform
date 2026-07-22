using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Attendance.Common
{
    /// <summary>
    /// Attendance rate = present/late count over sessions held within the enrollment's
    /// cohort window (start date through end date + grace period — see
    /// docs/DATABASE.md §4), excluding excused sessions from the denominator so an
    /// excused absence never counts against the student. Unmarked sessions have no
    /// matching status and are implicitly treated as not attended.
    /// </summary>
    public static class AttendanceRateCalculator
    {
        public record Result(int EffectiveHeld, int PresentCount, decimal Rate);

        public static Result Calculate(int heldSessionsCount, IReadOnlyList<string> recordedStatuses)
        {
            var excusedCount = recordedStatuses.Count(s => s == AttendanceStatuses.Excused);
            var presentCount = recordedStatuses.Count(s => s == AttendanceStatuses.Present || s == AttendanceStatuses.Late);
            var effectiveHeld = heldSessionsCount - excusedCount;
            var rate = effectiveHeld <= 0 ? 0m : Math.Round((decimal)presentCount / effectiveHeld * 100m, 1);
            return new Result(effectiveHeld, presentCount, rate);
        }
    }
}
