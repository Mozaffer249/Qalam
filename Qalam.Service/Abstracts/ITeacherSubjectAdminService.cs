using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Results;

namespace Qalam.Service.Abstracts;

public interface ITeacherSubjectAdminService
{
    Task<List<AdminTeacherSubjectDto>> GetTeacherSubjectsForAdminAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<AdminTeacherSubjectDto?> GetTeacherSubjectForAdminAsync(int teacherId, int teacherSubjectId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<AdminTeacherSubjectDto>> GetTeacherSubjectsListAsync(
        int pageNumber,
        int pageSize,
        int? teacherId = null,
        int? subjectId = null,
        bool? isActive = null,
        DocumentVerificationStatus? verificationStatus = null,
        CancellationToken cancellationToken = default);
    Task<TeacherSubjectSummaryDto> GetSubjectSummaryAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<bool> InactivateSubjectAsync(int teacherId, int teacherSubjectId, int adminId, CancellationToken cancellationToken = default);
    Task<bool> ActivateSubjectAsync(int teacherId, int teacherSubjectId, int adminId, CancellationToken cancellationToken = default);
    Task<bool> RejectSubjectAsync(int teacherId, int teacherSubjectId, int adminId, string reason, CancellationToken cancellationToken = default);
    Task<bool> ApproveSubjectAsync(int teacherId, int teacherSubjectId, int adminId, CancellationToken cancellationToken = default);
    Task<bool> RestoreSubjectAsync(int teacherId, int teacherSubjectId, int adminId, CancellationToken cancellationToken = default);
}
