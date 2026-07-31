using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using CodeForge.Application.Coupons.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.Coupons.GetCoupons
{
    public class GetCouponsQueryHandler : IRequestHandler<GetCouponsQuery, PagedResult<CouponDto>>
    {
        private readonly ICodeForgeDbContext _context;

        public GetCouponsQueryHandler(ICodeForgeDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<CouponDto>> Handle(GetCouponsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Coupons.AsNoTracking().Include(x => x.CreatedBy).AsQueryable();

            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var coupons = await query
                .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var items = coupons.Select(CouponMapping.ToDto).ToList();

            return new PagedResult<CouponDto>(items, request.Page, request.PageSize, totalCount);
        }
    }
}
