using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Queries.GetTeacherAreas;

public class GetTeacherAreasQuery : IRequest<Response<List<TeacherAreaResponseDto>>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }
}
