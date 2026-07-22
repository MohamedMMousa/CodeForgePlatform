using CodeForge.Application.Cohorts.Common;
using CodeForge.Application.Common;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Cohorts.CreateCohort
{
    public class CreateCohortCommandHandler : IRequestHandler<CreateCohortCommand, CohortListDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateCohortCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CohortListDto> Handle(CreateCohortCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);

            if (course is null)
            {
                throw new KeyNotFoundException("Course was not found.");
            }

            var cohort = new Cohort
            {
                CourseId = course.Id,
                Course = course,
                Name = request.Name.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                EnrollmentCutoffDate = request.EnrollmentCutoffDate,
                Capacity = request.Capacity,
                GracePeriodDays = request.GracePeriodDays,
                Status = CohortStatuses.Draft
            };

            _context.Cohorts.Add(cohort);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "cohort.created", nameof(Cohort), cohort.Id,
                new { courseId = course.Id, cohort.Name, cohort.Capacity }));

            await _context.SaveChangesAsync(cancellationToken);

            return CohortMapping.ToDto(cohort, enrolledCount: 0, DateTime.UtcNow);
        }

        private Guid GetCurrentUserId()
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated admin could not be resolved.");
            }

            return userId;
        }
    }
}
