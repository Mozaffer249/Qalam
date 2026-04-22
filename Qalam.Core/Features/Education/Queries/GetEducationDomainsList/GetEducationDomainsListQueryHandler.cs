using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainsList;

public class GetEducationDomainsListQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationDomainsListQuery, Response<List<EducationDomainDto>>>
{
    private readonly IEducationDomainService _domainService;

    public GetEducationDomainsListQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<List<EducationDomainDto>>> Handle(
        GetEducationDomainsListQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _domainService.GetPaginatedDomainsAsync(
            request.PageNumber,
            request.PageSize,
            request.Search);

        return Success(
            entity: result.Items,
            Meta: BuildPaginationMeta(result.PageNumber, result.PageSize, result.TotalCount));
    }
}
