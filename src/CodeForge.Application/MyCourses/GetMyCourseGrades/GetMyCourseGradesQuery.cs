using CodeForge.Application.Gradebook.Common;
using MediatR;

namespace CodeForge.Application.MyCourses.GetMyCourseGrades
{
    public record MyCourseGradesDto(
        Guid CourseId,
        string CourseTitle,
        decimal AttendanceRate,
        List<AssessmentGradeDto> Assessments,
        List<AssignmentGradeDto> Assignments);

    public record GetMyCourseGradesQuery(Guid CourseId) : IRequest<MyCourseGradesDto>;
}
