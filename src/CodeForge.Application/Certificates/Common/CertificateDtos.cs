namespace CodeForge.Application.Certificates.Common
{
    // One row in the admin/instructor "who is eligible?" list for a course. Recommended
    // tier is computed live from current attendance + assessment data; ExistingCertificate
    // is non-null once a certificate has been issued for that enrollment.
    public record CertificateCandidateDto(
        Guid EnrollmentId,
        Guid StudentId,
        string StudentName,
        string StudentEmail,
        Guid CohortId,
        string CohortName,
        decimal AttendanceRate,
        decimal AttendanceThreshold,
        bool AttendanceMet,
        bool AssessmentsPassed,
        int RequiredAssessmentCount,
        string RecommendedTier,
        CertificateDto? ExistingCertificate);

    public record CourseCertificateCandidatesDto(
        Guid CourseId,
        string CourseTitle,
        IReadOnlyList<CertificateCandidateDto> Candidates);

    // Full certificate as seen by its owner student, an admin, or an instructor.
    public record CertificateDto(
        Guid Id,
        Guid EnrollmentId,
        Guid StudentId,
        string StudentName,
        Guid CourseId,
        string CourseTitle,
        Guid CohortId,
        string CohortName,
        string Tier,
        string SerialNumber,
        string VerificationCode,
        decimal AttendanceRate,
        bool AssessmentsPassed,
        string IssuedByName,
        DateTime IssuedAt,
        bool IsRevoked,
        DateTime? RevokedAt,
        string? RevocationReason);

    // Minimal, privacy-conscious payload for the PUBLIC verify-by-code endpoint. No IDs,
    // no email, no internal metrics beyond what a printed certificate already shows.
    public record CertificateVerificationDto(
        bool Found,
        bool IsValid,
        string? StudentName,
        string? CourseTitle,
        string? Tier,
        string? SerialNumber,
        DateTime? IssuedAt,
        bool IsRevoked);
}
