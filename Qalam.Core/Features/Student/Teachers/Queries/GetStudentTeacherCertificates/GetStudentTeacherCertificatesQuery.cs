using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Teacher;

namespace Qalam.Core.Features.Student.Teachers.Queries.GetStudentTeacherCertificates;

public class GetStudentTeacherCertificatesQuery : IRequest<Response<List<StudentTeacherCertificateDto>>>
{
    public int TeacherId { get; set; }
    public int Take { get; set; } = 10;
}
