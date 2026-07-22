using CodeForge.Application.Certificates.GetCertificateById;
using CodeForge.Application.Certificates.GetCourseCertificateCandidates;
using CodeForge.Application.Certificates.GetMyCertificates;
using CodeForge.Application.Certificates.IssueCertificate;
using CodeForge.Application.Certificates.RevokeCertificate;
using CodeForge.Application.Certificates.VerifyCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class CertificatesController : ControllerBase
    {
        private readonly ISender _sender;

        public CertificatesController(ISender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// Certificate eligibility roster for a course (admin or assigned instructor):
        /// every certifiable enrollment with its computed recommended tier and any
        /// already-issued certificate.
        /// </summary>
        [HttpGet("courses/{courseId:guid}/certificate-candidates")]
        public async Task<IActionResult> GetCandidates(Guid courseId, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCourseCertificateCandidatesQuery(courseId), cancellationToken);
            return Ok(response);
        }

        /// <summary>Issue a certificate for an enrollment. Admin only.</summary>
        [HttpPost("certificates")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Issue(IssueCertificateRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new IssueCertificateCommand(request.EnrollmentId, request.Tier), cancellationToken);
            return Ok(response);
        }

        /// <summary>Revoke a previously issued certificate. Admin only.</summary>
        [HttpPut("certificates/{id:guid}/revoke")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Revoke(Guid id, RevokeCertificateRequest request, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new RevokeCertificateCommand(id, request.Reason), cancellationToken);
            return Ok(response);
        }

        /// <summary>The current student's own certificates.</summary>
        [HttpGet("my-certificates")]
        public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetMyCertificatesQuery(), cancellationToken);
            return Ok(response);
        }

        /// <summary>A single certificate — owner student, admin, or the course's instructor.</summary>
        [HttpGet("certificates/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new GetCertificateByIdQuery(id), cancellationToken);
            return Ok(response);
        }

        /// <summary>Public verification by the certificate's verification code. No auth.</summary>
        [HttpGet("certificates/verify/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string code, CancellationToken cancellationToken)
        {
            var response = await _sender.Send(new VerifyCertificateQuery(code), cancellationToken);
            return Ok(response);
        }

        public record IssueCertificateRequest(Guid EnrollmentId, string? Tier);

        public record RevokeCertificateRequest(string? Reason);
    }
}
