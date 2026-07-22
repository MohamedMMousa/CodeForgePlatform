using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Common
{
    /// <summary>
    /// Shared authorization checks for course content (modules/sessions/materials/
    /// announcements): admin always allowed; instructor allowed only if assigned to
    /// the course; student allowed to *view* only with an active enrollment.
    /// Requires the course's Instructors (and, for view checks, Enrollments)
    /// collections to already be loaded.
    /// </summary>
    public static class CourseContentAuthorization
    {
        public static void EnsureCanManage(
            ICurrentUserService currentUser,
            Course course,
            Guid currentUserId)
        {
            if (currentUser.Role == Roles.Admin)
            {
                return;
            }

            if (currentUser.Role == Roles.Instructor
                && course.Instructors.Any(i => i.InstructorId == currentUserId))
            {
                return;
            }

            throw new UnauthorizedAccessException("User does not have permission to manage this course's content.");
        }

        public static void EnsureCanView(
            ICurrentUserService currentUser,
            Course course,
            Guid currentUserId)
        {
            if (currentUser.Role == Roles.Admin)
            {
                return;
            }

            if (currentUser.Role == Roles.Instructor
                && course.Instructors.Any(i => i.InstructorId == currentUserId))
            {
                return;
            }

            if (currentUser.Role == Roles.Student
                && course.Enrollments.Any(e => e.StudentId == currentUserId && e.Status == EnrollmentStatuses.Active))
            {
                return;
            }

            throw new UnauthorizedAccessException("User does not have permission to view this course's content.");
        }
    }
}
