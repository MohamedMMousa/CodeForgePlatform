namespace CodeForge.Application.Analytics.Common
{
    // ---- Admin business dashboard ----

    public record MonthlyCountDto(int Year, int Month, int Count);

    public record RevenueByCourseDto(Guid CourseId, string Title, decimal Revenue, int ApprovedRequests);

    public record AdminBusinessDashboardDto(
        int TotalStudents,
        int PublishedCourses,
        int PublishedTracks,
        int ActiveEnrollments,
        int PendingEnrollmentRequests,
        decimal TotalRevenue,
        int TotalLeads,
        int UncontactedLeads,
        int OpenCohorts,
        IReadOnlyList<RevenueByCourseDto> TopCoursesByRevenue,
        IReadOnlyList<MonthlyCountDto> EnrollmentsByMonth);

    // ---- Admin academic dashboard ----

    public record CourseAcademicRowDto(
        Guid CourseId,
        string Title,
        int ActiveEnrollments,
        int Assessments,
        int SubmittedAttempts,
        decimal AssessmentPassRate,
        int CertificatesIssued);

    public record AdminAcademicDashboardDto(
        int CertificatesIssued,
        int CompletionCertificates,
        int ParticipationCertificates,
        int RevokedCertificates,
        int TotalAssessments,
        int SubmittedAttempts,
        decimal AssessmentPassRate,
        int TotalAssignments,
        int TotalSubmissions,
        IReadOnlyList<CourseAcademicRowDto> Courses);

    // ---- Instructor dashboard ----

    public record InstructorCourseRowDto(
        Guid CourseId,
        string Title,
        string Status,
        int ActiveEnrollments,
        int Assessments,
        int SubmittedAttempts,
        decimal AssessmentPassRate,
        int CertificatesIssued);

    public record InstructorDashboardDto(
        int AssignedCourses,
        int TotalActiveStudents,
        int CertificatesIssued,
        IReadOnlyList<InstructorCourseRowDto> Courses);
}
