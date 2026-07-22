using CodeForge.Application.Certificates.Common;
using MediatR;

namespace CodeForge.Application.Certificates.GetCourseCertificateCandidates
{
    public record GetCourseCertificateCandidatesQuery(Guid CourseId) : IRequest<CourseCertificateCandidatesDto>;
}
