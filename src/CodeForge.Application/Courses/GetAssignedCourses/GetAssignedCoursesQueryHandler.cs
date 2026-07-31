using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetAssignedCourses
{
    public class GetAssignedCoursesQueryHandler
        : IRequestHandler<GetAssignedCoursesQuery, PagedResult<CourseListDto>>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetAssignedCoursesQueryHandler(
            ICodeForgeDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<PagedResult<CourseListDto>> Handle(
            GetAssignedCoursesQuery request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var instructorId))
            {
                throw new UnauthorizedAccessException("Authenticated instructor could not be resolved.");
            }

            var query = _context.CourseInstructors
                .AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.InstructorId == instructorId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Course.CreatedAt).ThenBy(x => x.Course.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(x => CourseMapping.ToListDto(x.Course))
                .ToListAsync(cancellationToken);

            return new PagedResult<CourseListDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
