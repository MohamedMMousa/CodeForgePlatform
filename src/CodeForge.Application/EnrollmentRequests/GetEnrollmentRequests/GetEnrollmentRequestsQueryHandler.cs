using CodeForge.Application.EnrollmentRequests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CodeForge.Application.Common.Interfaces;

namespace CodeForge.Application.EnrollmentRequests.GetEnrollmentRequests
{
    public class GetEnrollmentRequestsQueryHandler
        : IRequestHandler<GetEnrollmentRequestsQuery, IReadOnlyList<EnrollmentRequestDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetEnrollmentRequestsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<EnrollmentRequestDto>> Handle(
            GetEnrollmentRequestsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.EnrollmentRequests
                .AsNoTracking()
                .Include(x => x.Course)
                .Include(x => x.Track)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.Trim().ToLower();
                query = query.Where(x => x.Status == status);
            }

            if (request.CourseId.HasValue)
            {
                query = query.Where(x => x.CourseId == request.CourseId.Value);
            }

            if (request.TrackId.HasValue)
            {
                query = query.Where(x => x.TrackId == request.TrackId.Value);
            }

            var requests = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);

            return requests
                .Select(x => new EnrollmentRequestDto(
                    x.Id,
                    x.ApplicantName,
                    x.ApplicantEmail,
                    x.ApplicantPhone,
                    x.CourseId,
                    x.Course?.Title,
                    x.TrackId,
                    x.Track?.Title,
                    x.PaymentMethod,
                    $"/enrollment-requests/{x.Id}/payment-proof",
                    x.OriginalPrice,
                    x.CouponCode,
                    x.DiscountAmount,
                    x.FinalPrice,
                    x.Status,
                    x.CreatedAt,
                    x.UpdatedAt))
                .ToList();
        }
    }
}
