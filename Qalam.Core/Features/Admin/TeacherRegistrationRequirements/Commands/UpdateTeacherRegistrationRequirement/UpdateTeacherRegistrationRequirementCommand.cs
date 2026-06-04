using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.UpdateTeacherRegistrationRequirement;

public class UpdateTeacherRegistrationRequirementCommand : IRequest<Response<TeacherRegistrationRequirementAdminDto>>
{
    public int Id { get; set; }
    public UpdateTeacherRegistrationRequirementDto Data { get; set; } = null!;
}
