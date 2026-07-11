using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainById;

public class GetEducationDomainByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationDomainByIdQuery, Response<EducationDomainDto>>
{
    private readonly IEducationDomainService _domainService;

    public GetEducationDomainByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomainDto>> Handle(
        GetEducationDomainByIdQuery request,
        CancellationToken cancellationToken)
    {
        var domain = await _domainService.GetDomainDtoByIdAsync(request.Id);

        if (domain == null)
            return NotFound<EducationDomainDto>("Education domain not found");

        return Success(entity: domain);
    }
}
