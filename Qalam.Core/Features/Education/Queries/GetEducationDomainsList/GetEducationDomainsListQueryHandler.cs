using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Data.Results;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainsList;

public class GetEducationDomainsListQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationDomainsListQuery, Response<PaginatedResult<EducationDomain>>>
{
    private readonly IEducationDomainService _domainService;

    public GetEducationDomainsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<PaginatedResult<EducationDomain>>> Handle(
        GetEducationDomainsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _domainService.GetPaginatedDomainsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(entity: result);
    }
}
