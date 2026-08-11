using MediatR;
using Microsoft.Extensions.Localization;
using Qalam.Core.Bases;
using Qalam.Core.Resources.Shared;
using Qalam.Service.Abstracts;

namespace Qalam.Core.Features.Authentication.Commands.UpdateMyProfilePicture;

public class UpdateMyProfilePictureCommandHandler : ResponseHandler,
    IRequestHandler<UpdateMyProfilePictureCommand, Response<UpdateMyProfilePictureResponse>>
{
    private readonly IUserProfileService _userProfileService;

    public UpdateMyProfilePictureCommandHandler(
        IUserProfileService userProfileService,
        IStringLocalizer<SharedResources> localizer) : base(localizer)
    {
        _userProfileService = userProfileService;
    }

    public async Task<Response<UpdateMyProfilePictureResponse>> Handle(
        UpdateMyProfilePictureCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UpdateProfilePictureAsync(
            request.UserId,
            request.File,
            cancellationToken);

        if (result.NotFound)
            return NotFound<UpdateMyProfilePictureResponse>(result.Error ?? "User not found.");

        if (!result.Succeeded)
            return BadRequest<UpdateMyProfilePictureResponse>(
                result.Error ?? "Unable to update profile picture.");

        return Success(
            Message: "Profile picture upload queued.",
            entity: new UpdateMyProfilePictureResponse
            {
                ProfilePictureUrl = result.ProfilePictureUrl,
            });
    }
}
