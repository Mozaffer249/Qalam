using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Queries.GetEducationDomainById;

public class GetEducationDomainByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetEducationDomainByIdQuery, Response<EducationDomain>>
{
    private readonly IEducationDomainService _domainService;

    public GetEducationDomainByIdQueryHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomain>> Handle(
        GetEducationDomainByIdQuery request,
        CancellationToken cancellationToken)
    {
        var domain = await _domainService.GetDomainWithLevelsAsync(request.Id);
        
        if (domain == null)
            return NotFound<EducationDomain>("Education domain not found");

        return Success(entity: domain);
    }
}
