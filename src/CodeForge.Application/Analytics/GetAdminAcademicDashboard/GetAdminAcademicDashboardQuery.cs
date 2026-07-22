using CodeForge.Application.Analytics.Common;
using MediatR;

namespace CodeForge.Application.Analytics.GetAdminAcademicDashboard
{
    public record GetAdminAcademicDashboardQuery() : IRequest<AdminAcademicDashboardDto>;
}
