using Qalam.Data.DTOs.Complaint;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Complaint;

namespace Qalam.Infrastructure.Abstracts;

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Complaint?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<Complaint?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<(List<Complaint> Items, int Total)> ListAsync(
        ComplaintListFilter filter,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ComplaintCountsDto> GetCountsAsync(
        ComplaintListFilter? filter = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasOpenForSubjectAsync(
        ComplaintSubjectType subjectType,
        int complainantUserId,
        int? courseScheduleId,
        int? enrollmentId,
        int? paymentId,
        int? refundId,
        int? openSessionRequestId,
        CancellationToken cancellationToken = default);

    Task<int> CountOpenForPartiesAsync(
        int complainantUserId,
        int? respondentTeacherId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Complaint complaint, CancellationToken cancellationToken = default);

    Task AddTimelineAsync(ComplaintTimelineEntry entry, CancellationToken cancellationToken = default);

    Task AddAttachmentAsync(ComplaintAttachment attachment, CancellationToken cancellationToken = default);

    Task RemoveAttachmentAsync(int attachmentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
