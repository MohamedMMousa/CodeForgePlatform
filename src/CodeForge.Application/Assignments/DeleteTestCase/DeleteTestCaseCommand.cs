using CodeForge.Application.Assignments.Common;
using MediatR;

namespace CodeForge.Application.Assignments.DeleteTestCase
{
    public record DeleteTestCaseCommand(Guid Id) : IRequest<TestCaseResponseDto>;
}
