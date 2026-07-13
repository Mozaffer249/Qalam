using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnits;

public class GetTeacherSubjectUnitsQuery : IRequest<Response<List<TeacherSubjectUnitOptionDto>>>, IAuthenticatedRequest
{
    public int TeacherSubjectId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
