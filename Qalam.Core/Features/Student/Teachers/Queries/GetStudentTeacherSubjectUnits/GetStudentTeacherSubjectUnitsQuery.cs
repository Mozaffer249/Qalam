using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjectUnits;

public class GetStudentTeacherSubjectUnitsQuery : IRequest<Response<List<TeacherSubjectUnitOptionDto>>>
{
    public int TeacherId { get; set; }
    public int TeacherSubjectId { get; set; }
}
