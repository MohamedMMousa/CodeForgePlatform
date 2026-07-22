using CodeForge.Application.Analytics.Common;
using MediatR;

namespace CodeForge.Application.Analytics.GetAdminBusinessDashboard
{
    public record GetAdminBusinessDashboardQuery() : IRequest<AdminBusinessDashboardDto>;
}
