using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Profile.Queries.GetMyTeacherProfile;

public class GetMyTeacherProfileQuery : IRequest<Response<TeacherMyProfileDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
