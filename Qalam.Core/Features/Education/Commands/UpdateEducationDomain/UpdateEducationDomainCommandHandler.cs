using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.Entity.Education;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Education.Commands.UpdateEducationDomain;

public class UpdateEducationDomainCommandHandler : ResponseHandler,
    IRequestHandler<UpdateEducationDomainCommand, Response<EducationDomain>>
{
    private readonly IEducationDomainService _domainService;

    public UpdateEducationDomainCommandHandler(
        IStringLocalizer<SharedResources> localizer,
        IEducationDomainService domainService) : base(localizer)
    {
        _domainService = domainService;
    }

    public async Task<Response<EducationDomain>> Handle(
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
                HasCurriculum = request.HasCurriculum,
                IsActive = request.IsActive
            };

            var result = await _domainService.UpdateDomainAsync(domain);
            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<EducationDomain>(ex.Message);
        }
    }
}
