using CodeForge.Domain.Entities;

namespace CodeForge.Application.Certificates.Common
{
    public static class CertificateMapping
    {
        // Requires Student, Course, Cohort, and IssuedBy navigations to be loaded.
        public static CertificateDto ToDto(Certificate certificate)
        {
            return new CertificateDto(
                certificate.Id,
                certificate.EnrollmentId,
                certificate.StudentId,
                certificate.Student.FullName,
                certificate.CourseId,
                certificate.Course.Title,
                certificate.CohortId,
                certificate.Cohort.Name,
                certificate.Tier,
                certificate.SerialNumber,
                certificate.VerificationCode,
                certificate.AttendanceRate,
                certificate.AssessmentsPassed,
                certificate.IssuedBy.FullName,
                certificate.IssuedAt,
                certificate.IsRevoked,
                certificate.RevokedAt,
                certificate.RevocationReason);
        }
    }
}
