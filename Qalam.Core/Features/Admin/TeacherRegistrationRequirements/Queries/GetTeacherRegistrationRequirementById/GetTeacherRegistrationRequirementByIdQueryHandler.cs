using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.GetTeacherRegistrationRequirementById;

public class GetTeacherRegistrationRequirementByIdQueryHandler : ResponseHandler,
    IRequestHandler<GetTeacherRegistrationRequirementByIdQuery, Response<TeacherRegistrationRequirementAdminDto>>
{
    private readonly ITeacherRegistrationRequirementRepository _repository;
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public GetTeacherRegistrationRequirementByIdQueryHandler(
        ITeacherRegistrationRequirementRepository repository,
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherRegistrationRequirementAdminDto>> Handle(
        GetTeacherRegistrationRequirementByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<TeacherRegistrationRequirementAdminDto>("Requirement not found");

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
