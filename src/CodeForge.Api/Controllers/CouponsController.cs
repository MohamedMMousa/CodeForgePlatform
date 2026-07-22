using CodeForge.Application.Coupons.CreateCoupon;
using CodeForge.Application.Coupons.DeactivateCoupon;
using CodeForge.Application.Coupons.GetCouponById;
using CodeForge.Application.Coupons.GetCoupons;
using CodeForge.Application.Coupons.UpdateCoupon;
using CodeForge.Application.Coupons.ValidateCoupon;
using CodeForge.Api.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Route("coupons")]
    [Produces("application/json")]
    public class CouponsController : ControllerBase
    {
        private readonly ISender _sender;

        public CouponsController(ISender sender)
        {
            _sender = sender;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateCouponRequest request, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(
                new CreateCouponCommand(
                    request.Code, request.Type, request.Value,
                    request.ValidFrom, request.ValidUntil, request.UsageLimit),
                cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateCouponRequest request, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(
                new UpdateCouponCommand(
                    id, request.Type, request.Value, request.IsActive,
                    request.ValidFrom, request.ValidUntil, request.UsageLimit),
                cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(new DeactivateCouponCommand(id), cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(new GetCouponsQuery(isActive), cancellationToken);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(new GetCouponByIdQuery(id), cancellationToken);
        }

        /// <summary>
        /// Public discount preview for the enroll form. Does not consume coupon usage.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.PublicSubmit)]
        [HttpPost("validate")]
        public async Task<IActionResult> Validate(ValidateCouponRequest request, CancellationToken cancellationToken)
        {
            return await SendCouponRequest(
                new ValidateCouponQuery(request.Code, request.CourseId, request.TrackId),
                cancellationToken);
        }

        private async Task<IActionResult> SendCouponRequest<TResponse>(
            IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(request, cancellationToken);
            return Ok(response);
        }

        public record CreateCouponRequest(
            string Code, string Type, decimal Value,
            DateTime? ValidFrom, DateTime? ValidUntil, int? UsageLimit);

        public record UpdateCouponRequest(
            string Type, decimal Value, bool IsActive,
            DateTime? ValidFrom, DateTime? ValidUntil, int? UsageLimit);

        public record ValidateCouponRequest(string Code, Guid? CourseId, Guid? TrackId);
    }
}
