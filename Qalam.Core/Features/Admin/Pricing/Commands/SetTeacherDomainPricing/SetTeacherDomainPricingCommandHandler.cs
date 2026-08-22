using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Pricing;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.Pricing.Commands.SetTeacherDomainPricing;

public class SetTeacherDomainPricingCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherDomainPricingCommand, Response<TeacherDomainPricingAdminDto>>
{
    private readonly IPricingAdminService _pricingAdminService;

    public SetTeacherDomainPricingCommandHandler(
        IPricingAdminService pricingAdminService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _pricingAdminService = pricingAdminService;
    }

    public async Task<Response<TeacherDomainPricingAdminDto>> Handle(
        SetTeacherDomainPricingCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _pricingAdminService.SetTeacherDomainPricingAsync(
                request.TeacherId, request.Data, cancellationToken);
            if (result == null)
                return NotFound<TeacherDomainPricingAdminDto>("Teacher not found.");

            return Success(entity: result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest<TeacherDomainPricingAdminDto>(ex.Message);
        }
    }
}
