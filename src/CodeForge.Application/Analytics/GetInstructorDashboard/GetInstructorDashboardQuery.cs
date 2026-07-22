using CodeForge.Application.Analytics.Common;
using MediatR;

namespace CodeForge.Application.Analytics.GetInstructorDashboard
{
    public record GetInstructorDashboardQuery() : IRequest<InstructorDashboardDto>;
}
