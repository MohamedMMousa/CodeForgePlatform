using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.DeactivateCoupon
{
    public class DeactivateCouponCommandHandler : IRequestHandler<DeactivateCouponCommand, CouponDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeactivateCouponCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CouponDto> Handle(DeactivateCouponCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var coupon = await _context.Coupons
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (coupon is null)
            {
                throw new KeyNotFoundException("Coupon was not found.");
            }

            coupon.IsActive = false;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "coupon.deactivated", nameof(Coupon), coupon.Id, new { coupon.Code }));

            await _context.SaveChangesAsync(cancellationToken);

            return CouponMapping.ToDto(coupon);
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
