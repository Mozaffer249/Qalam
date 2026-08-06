using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.TeacherAreas.Commands.AddTeacherArea;

public class AddTeacherAreaCommand : IRequest<Response<TeacherAreaResponseDto>>, IAuthenticatedRequest
{
    [BindNever]
    public int UserId { get; set; }

    public int LocationId { get; set; }
    public decimal? MaxDistanceKm { get; set; }
}
