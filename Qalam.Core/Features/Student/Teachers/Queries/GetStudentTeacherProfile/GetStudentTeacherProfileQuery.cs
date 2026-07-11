using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherProfile;

public class GetStudentTeacherProfileQuery : IRequest<Response<StudentTeacherProfileDto>>
{
    public int TeacherId { get; set; }
}
