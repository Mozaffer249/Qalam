using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.GetTeacherRegistrationRequirementById;

public class GetTeacherRegistrationRequirementByIdQuery : IRequest<Response<TeacherRegistrationRequirementAdminDto>>
{
    public int Id { get; set; }
}
