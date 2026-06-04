using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Admin.TeacherRegistrationRequirements.Queries.ListTeacherRegistrationRequirements;

public class ListTeacherRegistrationRequirementsQuery : IRequest<Response<List<TeacherRegistrationRequirementAdminDto>>>
{
}
