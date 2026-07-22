using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetAssignedCourses
{
    public class GetAssignedCoursesQueryHandler
        : IRequestHandler<GetAssignedCoursesQuery, IReadOnlyList<CourseListDto>>
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

        public async Task<IReadOnlyList<CourseListDto>> Handle(
            GetAssignedCoursesQuery request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var instructorId))
            {
                throw new UnauthorizedAccessException("Authenticated instructor could not be resolved.");
            }

            return await _context.CourseInstructors
                .AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.InstructorId == instructorId)
                .OrderByDescending(x => x.Course.CreatedAt)
                .Select(x => CourseMapping.ToListDto(x.Course))
                .ToListAsync(cancellationToken);
        }
    }
}
