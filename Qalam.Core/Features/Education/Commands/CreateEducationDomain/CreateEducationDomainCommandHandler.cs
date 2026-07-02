using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Service;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.CreateEducationDomain;

public class CreateEducationDomainCommandHandler : ResponseHandler,
    IRequestHandler<CreateEducationDomainCommand, Response<EducationDomain>>
{
    private readonly IEducationDomainService _domainService;

    public CreateEducationDomainCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomain>> Handle(
        CreateEducationDomainCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var domain = new EducationDomain
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                Code = request.Code,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                IsActive = request.IsActive
            };

            var ruleDto = request.EducationRule ?? EducationRuleDefaults.ForDomainCode(request.Code);
            var result = await _domainService.CreateDomainAsync(domain, ruleDto);
            return Created(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<EducationDomain>(ex.Message);
        }
    }
}
