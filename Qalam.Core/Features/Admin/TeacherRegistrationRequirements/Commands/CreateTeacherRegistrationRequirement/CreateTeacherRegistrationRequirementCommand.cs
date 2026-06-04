using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.CreateTeacherRegistrationRequirement;

public class CreateTeacherRegistrationRequirementCommand : IRequest<Response<TeacherRegistrationRequirementAdminDto>>
{
    public CreateTeacherRegistrationRequirementDto Data { get; set; } = null!;
}
