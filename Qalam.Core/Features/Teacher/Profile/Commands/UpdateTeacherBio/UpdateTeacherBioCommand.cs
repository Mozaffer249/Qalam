using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Profile.Commands.UpdateTeacherBio;

public class UpdateTeacherBioCommand : IRequest<Response<TeacherMyProfileDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public string? Bio { get; set; }
}
