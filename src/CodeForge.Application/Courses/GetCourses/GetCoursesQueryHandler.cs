using CodeForge.Application.Courses.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetCourses
{
    public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, PagedResult<CourseListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCoursesQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CourseListDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Courses.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim().ToLower();
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim().ToLower();
                query = query.Where(x => x.Category != null && x.Category.ToLower() == category);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.Title.ToLower().Contains(search) ||
                    x.Slug.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => CourseMapping.ToListDto(x))
                .ToListAsync(cancellationToken);

            return new PagedResult<CourseListDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
