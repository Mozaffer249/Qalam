using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.GetTeacherSubjectsForAdmin;

public class GetTeacherSubjectsForAdminQuery : IRequest<Response<List<AdminTeacherSubjectDto>>>
{
    public int TeacherId { get; set; }
}
