using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Coupons.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.GetCoupons
{
    public class GetCouponsQueryHandler : IRequestHandler<GetCouponsQuery, IReadOnlyList<CouponDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCouponsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CouponDto>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Coupons.AsNoTracking().Include(x => x.CreatedBy).AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var coupons = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

            return coupons.Select(CouponMapping.ToDto).ToList();
        }
    }
}
