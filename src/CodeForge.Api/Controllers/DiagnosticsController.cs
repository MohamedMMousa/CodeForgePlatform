using CodeForge.Api.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CodeForge.Api.Controllers
{
    /// <summary>
    /// Ops-only endpoints, each gated by its own config flag that stays off in normal
    /// operation — see docs/DEPLOY.md. Bypasses the usual MediatR/CQRS-triplet
    /// convention deliberately: this inspects the request pipeline itself (headers,
    /// connection info), not domain state, so it sits in the same category as the
    /// /health endpoints in Program.cs rather than a business use case.
    /// </summary>
    [ApiController]
    [Route("diagnostics")]
    [Authorize(Policy = "AdminOnly")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ProxySettings _proxySettings;

        public DiagnosticsController(IOptions<ProxySettings> proxySettings)
        {
            _proxySettings = proxySettings.Value;
        }

        /// <summary>
        /// Reports the raw X-Forwarded-For header, the socket peer, and the IP the rate
        /// limiter would currently select for this request. Used once, post-deploy, to
        /// measure the real Vercel→Render proxy chain and set
        /// Proxy:TrustedProxyHopCount correctly instead of guessing at it — see
        /// ProxySettings. Gated on Proxy:EnableDiagnostics; 404s when that's off (rather
        /// than 403) so its existence isn't revealed by an authorization error either.
        /// </summary>
        [HttpGet("client-ip")]
        [ProducesResponseType(typeof(ClientIpDiagnosticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetClientIp()
        {
            if (!_proxySettings.EnableDiagnostics)
            {
                return NotFound();
            }

            var forwardedFor = Request.Headers.TryGetValue("X-Forwarded-For", out var values)
                ? values.ToString()
                : null;

            return Ok(new ClientIpDiagnosticsDto(
                forwardedFor,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ClientIpResolver.Resolve(HttpContext, _proxySettings)));
        }

        public record ClientIpDiagnosticsDto(string? ForwardedFor, string? SocketPeer, string ResolvedClientIp);
    }
}
