namespace CodeForge.Application.Common.Constants
{
    /// <summary>
    /// Platform-wide completion-certificate defaults, applied when a course does not
    /// override them (Course.CompletionAttendanceThreshold is null). See SRS.md §9.
    /// </summary>
    public static class CertificateDefaults
    {
        // Minimum attendance rate (%) required for a Completion certificate.
        public const decimal AttendanceThreshold = 75m;
    }
}
