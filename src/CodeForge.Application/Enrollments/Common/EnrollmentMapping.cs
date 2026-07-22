using CodeForge.Domain.Entities;

namespace CodeForge.Application.Enrollments.Common
{
    public static class EnrollmentMapping
    {
        public static EnrollmentDto ToDto(Enrollment enrollment)
        {
            return new EnrollmentDto(
                enrollment.Id,
                enrollment.StudentId,
                enrollment.Student.FullName,
                enrollment.Student.Email,
                enrollment.CourseId,
                enrollment.Course.Title,
                enrollment.CohortId,
                enrollment.Cohort.Name,
                enrollment.Status,
                enrollment.AccessExpiresAt,
                enrollment.CancelledAt,
                enrollment.CancellationReason,
                enrollment.CreatedAt);
        }
    }
}
