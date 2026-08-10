using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Commands.UpdateChild;

public class UpdateChildCommandHandler : ResponseHandler,
    IRequestHandler<UpdateChildCommand, Response<ChildStudentDto>>
{
    private readonly IGuardianChildrenService _guardianChildrenService;

    public UpdateChildCommandHandler(
        IGuardianChildrenService guardianChildrenService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianChildrenService = guardianChildrenService;
    }

    public async Task<Response<ChildStudentDto>> Handle(
        UpdateChildCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _guardianChildrenService.UpdateChildAsync(
            request.UserId,
            request.StudentId,
            request.Child,
            cancellationToken);

        if (result.NotFound)
            return NotFound<ChildStudentDto>(result.Error ?? "Child not found.");

        if (!result.Succeeded || result.Child == null)
            return BadRequest<ChildStudentDto>(result.Error ?? "Unable to update child.");

        return Success(Message: "Child updated successfully.", entity: result.Child);
    }
}
