using CodeForge.Application.Cohorts.Common;
using CodeForge.Domain.Entities;

namespace CodeForge.Application.Courses.Common
{
    public static class CourseMapping
    {
        public static CourseListDto ToListDto(Course course, NextCohortSummaryDto? nextCohort = null)
        {
            return new CourseListDto(
                course.Id,
                course.Title,
                course.Slug,
                course.Description,
                course.ThumbnailUrl,
                course.Category,
                course.Price,
                course.Currency,
                course.Status,
                course.CreatedAt,
                course.UpdatedAt,
                nextCohort);
        }

        public static CourseDetailDto ToDetailDto(Course course)
        {
            return new CourseDetailDto(
                course.Id,
                course.Title,
                course.Slug,
                course.Description,
                course.ThumbnailUrl,
                course.Category,
                course.Price,
                course.Currency,
                course.Status,
                course.CompletionAttendanceThreshold,
                course.CreatedById,
                course.CreatedBy.FullName,
                course.CreatedAt,
                course.UpdatedAt,
                course.Instructors
                    .OrderBy(x => x.Instructor.FullName)
                    .Select(ToInstructorDto)
                    .ToList());
        }

        public static CourseInstructorDto ToInstructorDto(CourseInstructor courseInstructor)
        {
            return new CourseInstructorDto(
                courseInstructor.Id,
                courseInstructor.InstructorId,
                courseInstructor.Instructor.FullName,
                courseInstructor.Instructor.Email,
                courseInstructor.AssignedAt);
        }
    }
}
