using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Infrastructure.Abstracts;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.DeleteTeacherRegistrationRequirement;

public class DeleteTeacherRegistrationRequirementCommandHandler : ResponseHandler,
    IRequestHandler<DeleteTeacherRegistrationRequirementCommand, Response<string>>
{
    private readonly ITeacherRegistrationRequirementRepository _repository;

    public DeleteTeacherRegistrationRequirementCommandHandler(
        ITeacherRegistrationRequirementRepository repository,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _repository = repository;
    }

    public async Task<Response<string>> Handle(
        DeleteTeacherRegistrationRequirementCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.Id);
        if (entity == null)
            return NotFound<string>("Requirement not found");

        if (entity.IsSystem)
            return BadRequest<string>("System requirements cannot be deleted. Deactivate instead.");

        if (await _repository.HasSubmissionsAsync(request.Id, cancellationToken))
            return BadRequest<string>("Cannot delete: teachers have already submitted this requirement.");

        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();

        return Success<string>("Requirement deleted");
    }
}
