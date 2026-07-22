using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Users.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Users.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetUsersQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
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

            var users = await query
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);

            return users.Select(UserMapping.ToDto).ToList();
        }
    }
}
