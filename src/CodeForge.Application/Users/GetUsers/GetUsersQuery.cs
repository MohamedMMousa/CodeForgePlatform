using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Users.Common;
using MediatR;

namespace CodeForge.Application.Users.GetUsers
{
    public record GetUsersQuery(
        string? Role,
        bool? IsActive,
        string? Search,
        int Page = PaginationDefaults.Page,
        int PageSize = PaginationDefaults.PageSize) : IRequest<PagedResult<UserDto>>;
}
