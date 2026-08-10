using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Data.DTOs.Student;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Student.Commands.UpdateChildProfilePicture;

public class UpdateChildProfilePictureCommandHandler : ResponseHandler,
    IRequestHandler<UpdateChildProfilePictureCommand, Response<ChildStudentDto>>
{
    private readonly IGuardianChildrenService _guardianChildrenService;

    public UpdateChildProfilePictureCommandHandler(
        IGuardianChildrenService guardianChildrenService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _guardianChildrenService = guardianChildrenService;
    }

    public async Task<Response<ChildStudentDto>> Handle(
        UpdateChildProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _guardianChildrenService.UpdateProfilePictureAsync(
            request.UserId,
            request.StudentId,
            request.File,
            cancellationToken);

        if (result.NotFound)
            return NotFound<ChildStudentDto>(result.Error ?? "Child not found.");

        if (!result.Succeeded || result.Child == null)
            return BadRequest<ChildStudentDto>(result.Error ?? "Unable to update profile picture.");

        return Success(
            Message: "Profile picture upload queued.",
            entity: result.Child);
    }
}
