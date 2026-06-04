using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;

public class GetTeacherRegistrationRequirementsQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherRegistrationRequirementsQuery, Response<TeacherRegistrationRequirementsResponseDto>>
{
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public GetTeacherRegistrationRequirementsQueryHandler(
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<TeacherRegistrationRequirementsResponseDto>> Handle(
        GetTeacherRegistrationRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var requirements = await _provider.GetActivePublicDtosAsync(cancellationToken);
        return Success(entity: new TeacherRegistrationRequirementsResponseDto { Requirements = requirements });
    }
}
