using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.TeachingPreferences.Queries.GetTeacherTeachingPreferences;

public class GetTeacherTeachingPreferencesQuery : IRequest<Response<TeacherTeachingPreferencesDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
