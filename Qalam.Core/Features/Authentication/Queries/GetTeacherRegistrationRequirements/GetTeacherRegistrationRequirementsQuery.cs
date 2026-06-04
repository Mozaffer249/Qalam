using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Authentication.Queries.GetTeacherRegistrationRequirements;

public class GetTeacherRegistrationRequirementsQuery : IRequest<Response<TeacherRegistrationRequirementsResponseDto>>
{
}
