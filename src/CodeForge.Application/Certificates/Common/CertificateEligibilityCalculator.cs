using CodeForge.Application.Common.Constants;

namespace CodeForge.Application.Certificates.Common
{
    /// <summary>
    /// Two-tier certificate rule (SRS.md §9): a Completion certificate requires the
    /// attendance rate to meet the course's threshold (or the platform default when the
    /// course doesn't override it) AND every non-practice assessment to be passed (each
    /// by its own pass score). Anything short of that — enrolled/attended but below the
    /// bar — is a Participation certificate. Pure logic so it can be unit-tested and
    /// shared by the candidate-list query and the issue command.
    /// </summary>
    public static class CertificateEligibilityCalculator
    {
        public record Result(
            string Tier,
            bool AttendanceMet,
            bool AssessmentsPassed,
            decimal AttendanceRate,
            decimal AttendanceThreshold);

        public static decimal ResolveThreshold(decimal? courseThreshold)
            => courseThreshold ?? CertificateDefaults.AttendanceThreshold;

        /// <param name="requiredAssessmentPassStates">
        /// One bool per non-practice assessment — true iff the student passed it. The
        /// caller filters out practice assessments and treats never-attempted/failed as
        /// false. An empty list means the course has no required assessments, so the
        /// assessment bar is vacuously met.
        /// </param>
        public static Result Evaluate(
            decimal attendanceRate,
            decimal? courseThreshold,
            IReadOnlyList<bool> requiredAssessmentPassStates)
        {
            var threshold = ResolveThreshold(courseThreshold);
            var attendanceMet = attendanceRate >= threshold;
            var assessmentsPassed = requiredAssessmentPassStates.All(passed => passed);
            var tier = attendanceMet && assessmentsPassed
                ? CertificateTiers.Completion
                : CertificateTiers.Participation;

            return new Result(tier, attendanceMet, assessmentsPassed, attendanceRate, threshold);
        }
    }
}
