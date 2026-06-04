using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.SetTeacherRegistrationRequirementActive;

public class SetTeacherRegistrationRequirementActiveCommand : IRequest<Response<TeacherRegistrationRequirementAdminDto>>
{
    public int Id { get; set; }
    public SetRequirementActiveDto Data { get; set; } = null!;
}
