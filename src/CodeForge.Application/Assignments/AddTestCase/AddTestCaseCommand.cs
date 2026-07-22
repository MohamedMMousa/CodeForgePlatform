using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.AddTestCase
{
    public record AddTestCaseCommand(Guid AssignmentId, string Input, string ExpectedOutput, bool IsHidden, int Points)
        : IRequest<TestCaseResponseDto>;
}
