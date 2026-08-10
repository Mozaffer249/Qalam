using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Student;

namespace Qalam.Core.Features.Student.Commands.UpdateChildProfilePicture;

public class UpdateChildProfilePictureCommand
    : IRequest<Response<ChildStudentDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int StudentId { get; set; }
    public IFormFile File { get; set; } = null!;
}
