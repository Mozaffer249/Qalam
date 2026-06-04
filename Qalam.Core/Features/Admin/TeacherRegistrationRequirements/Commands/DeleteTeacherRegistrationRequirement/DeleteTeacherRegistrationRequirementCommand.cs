using MediatR;
using Qalam.Core.Bases;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Commands.DeleteTeacherRegistrationRequirement;

public class DeleteTeacherRegistrationRequirementCommand : IRequest<Response<string>>
{
    public int Id { get; set; }
}
