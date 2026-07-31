using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Users.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Users.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetUsersQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                var role = request.Role.Trim().ToLower();
                query = query.Where(x => x.Role == role);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(search) || x.Email.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderBy(x => x.FullName).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = users.Select(UserMapping.ToDto).ToList();

            return new PagedResult<UserDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
