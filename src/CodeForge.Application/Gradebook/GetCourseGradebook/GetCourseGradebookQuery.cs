using CodeForge.Application.Gradebook.Common;
using MediatR;

namespace CodeForge.Application.Gradebook.GetCourseGradebook
{
    public record StudentGradebookRowDto(
        Guid StudentId,
        string StudentName,
        decimal AttendanceRate,
        List<AssessmentGradeDto> Assessments,
        List<AssignmentGradeDto> Assignments);

    public record CourseGradebookDto(Guid CourseId, string CourseTitle, List<StudentGradebookRowDto> Students);

    public record GetCourseGradebookQuery(Guid CourseId) : IRequest<CourseGradebookDto>;
}
