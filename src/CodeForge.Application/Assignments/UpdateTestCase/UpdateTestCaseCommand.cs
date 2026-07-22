using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.UpdateTestCase
{
    public record UpdateTestCaseCommand(Guid Id, string Input, string ExpectedOutput, bool IsHidden, int Points)
        : IRequest<TestCaseResponseDto>;
}
