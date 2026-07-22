using CodeForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CodeForge.Application.EnrollmentRequests.GetPaymentProofFile
{
    public class GetPaymentProofFileQueryHandler : IRequestHandler<GetPaymentProofFileQuery, PaymentProofFileResult>
    {
        private readonly ICodeForgeDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        public GetPaymentProofFileQueryHandler(ICodeForgeDbContext context, IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
        }

        public async Task<PaymentProofFileResult> Handle(GetPaymentProofFileQuery request, CancellationToken cancellationToken)
        {
            var enrollmentRequest = await _context.EnrollmentRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.EnrollmentRequestId, cancellationToken);

            if (enrollmentRequest is null)
            {
                throw new KeyNotFoundException("Enrollment request was not found.");
            }

            var (stream, contentType) = await _fileStorageService.OpenPaymentProofAsync(
                enrollmentRequest.PaymentProofUrl, cancellationToken);

            return new PaymentProofFileResult(stream, contentType, $"payment-proof-{enrollmentRequest.Id}");
        }
    }
}
