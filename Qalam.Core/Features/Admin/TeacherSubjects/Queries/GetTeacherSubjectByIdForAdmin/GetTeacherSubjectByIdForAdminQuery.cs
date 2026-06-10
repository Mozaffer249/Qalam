using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectByIdForAdmin;

public class GetTeacherSubjectByIdForAdminQuery : IRequest<Response<AdminTeacherSubjectDto?>>
{
    public int TeacherId { get; set; }
    public int TeacherSubjectId { get; set; }
}
