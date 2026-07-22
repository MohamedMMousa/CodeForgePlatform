using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetCourseById
{
    public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDetailDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCourseByIdQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<CourseDetailDto> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Include(x => x.Instructors)
                .ThenInclude(x => x.Instructor)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            return CourseMapping.ToDetailDto(course);
        }
    }
}
