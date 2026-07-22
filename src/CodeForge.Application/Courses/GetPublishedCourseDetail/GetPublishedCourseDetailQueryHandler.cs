using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Courses.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Courses.GetPublishedCourseDetail
{
    public class GetPublishedCourseDetailQueryHandler
        : IRequestHandler<GetPublishedCourseDetailQuery, PublicCourseDetailDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetPublishedCourseDetailQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PublicCourseDetailDto> Handle(
            GetPublishedCourseDetailQuery request,
            CancellationToken cancellationToken)
        {
            var slug = request.Slug.Trim().ToLower();
            var course = await _context.Courses
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .Include(x => x.Instructors).ThenInclude(x => x.Instructor)
                .FirstOrDefaultAsync(
                    x => x.Slug == slug && x.Status == CourseStatuses.Published,
                    cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Published course was not found.");
            }

            // Only show cohorts a visitor could plausibly enroll in: open, or already
            // running/completed recently enough to be informative — exclude drafts.
            var cohorts = await _context.Cohorts
                .AsNoTracking()
                .Include(x => x.Course)
                .Where(x => x.CourseId == course.Id && x.Status != CohortStatuses.Draft)
                .OrderBy(x => x.StartDate)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var cohortDtos = new List<CohortListDto>();
            foreach (var cohort in cohorts)
            {
                var enrolledCount = await CohortAvailability.GetActiveEnrollmentCountAsync(
                    _context, cohort.Id, cancellationToken);
                cohortDtos.Add(CohortMapping.ToDto(cohort, enrolledCount, now));
            }

            return new PublicCourseDetailDto(
                course.Id,
                course.Title,
                course.Slug,
                course.Description,
                course.ThumbnailUrl,
                course.Category,
                course.Price,
                course.Currency,
                course.Instructors
                    .OrderBy(x => x.Instructor.FullName)
                    .Select(CourseMapping.ToInstructorDto)
                    .ToList(),
                cohortDtos);
        }
    }
}
