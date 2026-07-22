using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.GetCouponById
{
    public class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, CouponDto>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCouponByIdQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<CouponDto> Handle(GetCouponByIdQuery request, CancellationToken cancellationToken)
        {
            var coupon = await _context.Coupons
                .AsNoTracking()
                .Include(x => x.CreatedBy)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

            if (coupon is null)
            {
                throw new KeyNotFoundException("Coupon was not found.");
            }

            return CouponMapping.ToDto(coupon);
        }
    }
}
