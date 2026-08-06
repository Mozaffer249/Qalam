using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qalam.Core.Bases;
using Qalam.Core.Contracts;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Teacher.Queries.GetTeacherSubjectUnitOptionsBySubject;

public class GetTeacherSubjectUnitOptionsBySubjectQuery
    : IRequest<Response<List<TeacherSubjectUnitPickerDto>>>, IAuthenticatedRequest
{
    public int SubjectId { get; set; }

    [BindNever]
    public int UserId { get; set; }
}
