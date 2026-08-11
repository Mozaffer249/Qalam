using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;

namespace Qalam.Core.Features.Authentication.Commands.UpdateMyProfilePicture;

public class UpdateMyProfilePictureCommand
    : IRequest<Response<UpdateMyProfilePictureResponse>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public IFormFile File { get; set; } = null!;
}

public class UpdateMyProfilePictureResponse
{
    public string? ProfilePictureUrl { get; set; }
}
