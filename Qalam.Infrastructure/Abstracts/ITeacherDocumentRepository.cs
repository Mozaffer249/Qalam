using Qalam.Data.DTOs.Admin;
using Qalam.Data.DTOs.Teacher;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.InfrastructureBases;

namespace Qalam.Infrastructure.Abstracts;

public interface ITeacherDocumentRepository : IGenericRepositoryAsync<TeacherDocument>
{
    /// <summary>
    /// Returns <c>true</c> when no <c>TeacherDocument</c> row currently holds the given identity
    /// triple. Pass <paramref name="excludeTeacherId"/> when the caller is about to re-persist the
    /// same teacher's own identity (e.g. retry of <c>SubmitRegistrationRequirements</c>) so the
    /// teacher's prior pending row doesn't trip the uniqueness gate against itself.
    /// </summary>
    Task<bool> IsIdentityNumberUniqueAsync(
        IdentityType type,
        string number,
        string? countryCode,
        int? excludeTeacherId = null);

    Task<int> GetCertificateCountAsync(int teacherId);
    Task<IEnumerable<TeacherDocument>> GetTeacherDocumentsAsync(int teacherId);
    
    // Admin operations
    Task<List<TeacherDocument>> GetByTeacherIdAsync(int teacherId);
    Task<List<TeacherDocumentReviewDto>> GetDocumentsStatusAsync(int teacherId);
    
    // Teacher operations
    Task<List<RejectedDocumentInfo>> GetRejectedDocumentsAsync(int teacherId);

    /// <summary>
    /// Bulk-deletes every <c>TeacherDocument</c> still in <c>Pending</c> review for a teacher.
    /// Paired with <c>DeleteAllForTeacherAsync</c> on the submission repo to clear orphans from
    /// a prior partial submit before the handler inserts a fresh set. Approved/Rejected docs are
    /// preserved (they shouldn't exist at submit-time, but the filter is a safety net).
    /// </summary>
    Task<int> DeletePendingForTeacherAsync(int teacherId, CancellationToken cancellationToken = default);
}
