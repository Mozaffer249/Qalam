using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Teacher;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.SetTeacherRegistrationRequirementActive;

public class SetTeacherRegistrationRequirementActiveCommandHandler : ResponseHandler,
    IRequestHandler<SetTeacherRegistrationRequirementActiveCommand, Response<TeacherRegistrationRequirementAdminDto>>
{
    private readonly ITeacherRegistrationRequirementRepository _repository;
    private readonly ITeacherRegistrationRequirementProvider _provider;

    public SetTeacherRegistrationRequirementActiveCommandHandler(
        ITeacherRegistrationRequirementRepository repository,
        ITeacherRegistrationRequirementProvider provider,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<Response<TeacherRegistrationRequirementAdminDto>> Handle(
        SetTeacherRegistrationRequirementActiveCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<TeacherRegistrationRequirementAdminDto>("Requirement not found");

        entity.IsActive = request.Data.IsActive;
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();

        return Success(entity: _provider.ToAdminDto(entity));
    }
}
