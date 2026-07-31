using CodeForge.Application.Common.Constants;
using FluentValidation;

namespace CodeForge.Application.Certificates.GetMyCertificates
{
    public class GetMyCertificatesQueryValidator : AbstractValidator<GetMyCertificatesQuery>
    {
        public GetMyCertificatesQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, PaginationDefaults.MaxPageSize);
        }
    }
}
