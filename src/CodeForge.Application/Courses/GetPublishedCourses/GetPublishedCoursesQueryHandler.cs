using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetPublishedCourses
{
    public class GetPublishedCoursesQueryHandler
        : IRequestHandler<GetPublishedCoursesQuery, PagedResult<CourseListDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetPublishedCoursesQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CourseListDto>> Handle(
            GetPublishedCoursesQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Courses
                .AsNoTracking()
                .Where(x => x.Status == CourseStatuses.Published);

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
