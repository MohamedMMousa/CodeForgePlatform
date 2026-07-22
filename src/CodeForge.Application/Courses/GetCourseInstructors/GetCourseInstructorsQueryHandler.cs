using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetCourseInstructors
{
    public class GetCourseInstructorsQueryHandler
        : IRequestHandler<GetCourseInstructorsQuery, IReadOnlyList<CourseInstructorDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCourseInstructorsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CourseInstructorDto>> Handle(
            GetCourseInstructorsQuery request,
            CancellationToken cancellationToken)
        {
            var courseExists = await _context.Courses
                .AnyAsync(x => x.Id == request.CourseId, cancellationToken);

            if (!courseExists)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            return await _context.CourseInstructors
                .AsNoTracking()
                .Include(x => x.Instructor)
                .Where(x => x.CourseId == request.CourseId)
                .OrderBy(x => x.Instructor.FullName)
                .Select(x => CourseMapping.ToInstructorDto(x))
                .ToListAsync(cancellationToken);
        }
    }
}
