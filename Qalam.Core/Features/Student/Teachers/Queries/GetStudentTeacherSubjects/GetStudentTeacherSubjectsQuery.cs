using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherSubjects;

public class GetStudentTeacherSubjectsQuery : IRequest<Response<List<StudentTeacherSubjectDto>>>
{
    public int TeacherId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
