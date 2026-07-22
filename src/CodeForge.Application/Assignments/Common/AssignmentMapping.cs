using CodeForge.Domain.Entities;

namespace CodeForge.Application.Assignments.Common
{
    public static class AssignmentMapping
    {
        public static AssignmentDto ToDto(Assignment assignment)
        {
            return new AssignmentDto(
                assignment.Id,
                assignment.ModuleId,
                assignment.Title,
                assignment.Description,
                assignment.OrderIndex,
                assignment.IsPractice,
                assignment.MaxAttempts,
                assignment.DueAt,
                assignment.PassScore,
                assignment.TestCases.Count,
                assignment.CreatedAt,
                assignment.UpdatedAt);
        }

        public static AssignmentDetailDto ToDetailDto(Assignment assignment)
        {
            return new AssignmentDetailDto(
                assignment.Id,
                assignment.ModuleId,
                assignment.Title,
                assignment.Description,
                assignment.OrderIndex,
                assignment.IsPractice,
                assignment.MaxAttempts,
                assignment.DueAt,
                assignment.PassScore,
                assignment.TestCases
                    .OrderBy(tc => tc.OrderIndex)
                    .Select(tc => new TestCaseDto(tc.Id, tc.Input, tc.ExpectedOutput, tc.IsHidden, tc.Points, tc.OrderIndex))
                    .ToList());
        }
    }
}
