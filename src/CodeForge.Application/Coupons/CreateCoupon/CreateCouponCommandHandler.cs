using CodeForge.Application.Common;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using CodeForge.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.CreateCoupon
{
    public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, CouponDto>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CreateCouponCommandHandler(ICodeForgeDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<CouponDto> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
        {
            var adminId = GetCurrentUserId();
            var code = request.Code.Trim().ToUpperInvariant();

            var codeExists = await _context.Coupons.AnyAsync(x => x.Code == code, cancellationToken);
            if (codeExists)
            {
                throw new InvalidOperationException("A coupon with this code already exists.");
            }

            var coupon = new Coupon
            {
                Code = code,
                Type = request.Type,
                Value = request.Value,
                ValidFrom = request.ValidFrom,
                ValidUntil = request.ValidUntil,
                UsageLimit = request.UsageLimit,
                CreatedById = adminId
            };

            _context.Coupons.Add(coupon);
            _context.ActivityLogs.Add(ActivityLogFactory.Create(
                adminId, "coupon.created", nameof(Coupon), coupon.Id,
                new { coupon.Code, coupon.Type, coupon.Value }));

            await _context.SaveChangesAsync(cancellationToken);

            coupon.CreatedBy = await _context.Users.AsNoTracking().FirstAsync(x => x.Id == adminId, cancellationToken);

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
