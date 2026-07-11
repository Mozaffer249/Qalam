using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpdateEducationDomain;

public class UpdateEducationDomainCommandHandler : ResponseHandler,
    IRequestHandler<UpdateEducationDomainCommand, Response<EducationDomainDto>>
{
    private readonly IEducationDomainService _domainService;

    public UpdateEducationDomainCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomainDto>> Handle(
        UpdateEducationDomainCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var domain = new EducationDomain
            {
                Id = request.Id,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                Code = request.Code,
                DescriptionAr = request.DescriptionAr,
                DescriptionEn = request.DescriptionEn,
                IsActive = request.IsActive
            };

            await _domainService.UpdateDomainAsync(domain, request.EducationRule);
            var dto = await _domainService.GetDomainDtoByIdAsync(request.Id);
            if (dto == null)
                return NotFound<EducationDomainDto>("Education domain not found");

            return Success(entity: dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<EducationDomainDto>(ex.Message);
        }
    }
}
