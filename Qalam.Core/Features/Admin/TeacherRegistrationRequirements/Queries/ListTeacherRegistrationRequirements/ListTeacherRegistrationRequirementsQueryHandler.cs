using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.ListTeacherRegistrationRequirements;

public class ListTeacherRegistrationRequirementsQueryHandler : ResponseHandler,
    IRequestHandler<ListTeacherRegistrationRequirementsQuery, Response<List<TeacherRegistrationRequirementAdminDto>>>
{
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public ListTeacherRegistrationRequirementsQueryHandler(
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _provider = provider;
    }

    public async Task<Response<List<TeacherRegistrationRequirementAdminDto>>> Handle(
        ListTeacherRegistrationRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await _provider.GetAllRequirementsAsync(cancellationToken);
        var dtos = entities.Select(_provider.ToAdminDto).ToList();
        return Success(entity: dtos);
    }
}
