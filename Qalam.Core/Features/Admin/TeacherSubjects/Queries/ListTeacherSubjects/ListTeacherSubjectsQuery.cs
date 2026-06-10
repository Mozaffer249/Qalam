using MediatR;
using Qalam.Core.Bases;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Core.Features.Admin.TeacherSubjects.Queries.ListTeacherSubjects;

public class ListTeacherSubjectsQuery : IRequest<Response<List<AdminTeacherSubjectDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int? TeacherId { get; set; }
    public int? SubjectId { get; set; }
    public bool? IsActive { get; set; }
    public DocumentVerificationStatus? VerificationStatus { get; set; }
}
