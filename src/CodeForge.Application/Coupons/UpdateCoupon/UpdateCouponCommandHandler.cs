using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.UpdateCoupon
{
    public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, CouponDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UpdateCouponCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CouponDto> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var coupon = await _context.Coupons
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (coupon is null)
            {
                throw new KeyNotFoundException("Coupon was not found.");
            }

            coupon.Type = request.Type;
            coupon.Value = request.Value;
            coupon.IsActive = request.IsActive;
            coupon.ValidFrom = request.ValidFrom;
            coupon.ValidUntil = request.ValidUntil;
            coupon.UsageLimit = request.UsageLimit;

            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "coupon.updated", nameof(Coupon), coupon.Id,
                new { coupon.Code, coupon.Type, coupon.Value, coupon.IsActive }));

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
