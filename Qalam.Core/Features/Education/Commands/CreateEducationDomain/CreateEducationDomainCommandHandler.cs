using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.CreateEducationDomain;

public class CreateEducationDomainCommandHandler : ResponseHandler,
    IRequestHandler<CreateEducationDomainCommand, Response<EducationDomainDto>>
{
    private readonly IEducationDomainService _domainService;

    public CreateEducationDomainCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomainDto>> Handle(
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

            var result = await _domainService.CreateDomainAsync(domain, request.EducationRule);
            var dto = await _domainService.GetDomainDtoByIdAsync(result.Id);
            if (dto == null)
                return BadRequest<EducationDomainDto>("Domain was created but could not be loaded");

            return Created(entity: dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<EducationDomainDto>(ex.Message);
        }
    }
}
