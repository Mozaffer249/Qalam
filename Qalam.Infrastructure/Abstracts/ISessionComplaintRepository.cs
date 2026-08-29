using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface ISessionComplaintRepository
{
    Task<bool> HasBlockingComplaintAsync(int courseScheduleId, CancellationToken cancellationToken = default);

    Task<bool> HasOpenForStudentAsync(
        int courseScheduleId,
        int studentId,
        CancellationToken cancellationToken = default);

    Task<SessionComplaint?> GetByIdTrackedAsync(int complaintId, CancellationToken cancellationToken = default);

    Task<SessionComplaint?> GetByIdAsync(int complaintId, CancellationToken cancellationToken = default);

    Task<bool> BelongsToScheduleAsync(
        int complaintId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task<ComplaintSessionFinancialContextDto?> LoadFinancialContextAsync(
        int enrollmentId,
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task<string> GetPayoutImpactAsync(
        int enrollmentId,
        decimal refundAmount,
        CancellationToken cancellationToken = default);

    Task<SessionComplaint?> GetByIdForTeacherTrackedAsync(
        int complaintId,
        int teacherId,
        CancellationToken cancellationToken = default);

    Task<SessionComplaintDetailDto?> GetDetailAsync(
        int complaintId,
        int? studentId,
        CancellationToken cancellationToken = default);

    Task<List<SessionComplaint>> ListForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task AddComplaintAsync(SessionComplaint complaint, CancellationToken cancellationToken = default);

    Task AddAttachmentAsync(SessionComplaintAttachment attachment, CancellationToken cancellationToken = default);

    Task RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<TeacherEarningLine?> GetActiveEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task<TeacherEarningLine?> GetOnHoldEarningLineForScheduleAsync(
        int courseScheduleId,
        CancellationToken cancellationToken = default);

    Task UpdateEarningLineAsync(TeacherEarningLine line, CancellationToken cancellationToken = default);
}
