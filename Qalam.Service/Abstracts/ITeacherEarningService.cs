using Qalam.Data.Entity.Common.Enums;

namespace Qalam.Service.Abstracts;

public interface ITeacherEarningService
{
    /// <summary>
    /// Accrues a pending earning line for a completed schedule from the enrollment pricing snapshot.
    /// Idempotent per CourseScheduleId.
    /// </summary>
    Task AccrueForCompletedScheduleAsync(
        int courseScheduleId,
        TeacherEarningLineStatus initialStatus = TeacherEarningLineStatus.Pending,
        CancellationToken cancellationToken = default);
}

public interface IPayoutService
{
    Task<Data.DTOs.Admin.AdminPayoutBatchDto> CreateBatchFromPendingAsync(
        DateTime? periodStart,
        DateTime? periodEnd,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task<Data.DTOs.Admin.AdminPayoutBatchDto?> ApproveAsync(
        int batchId,
        CancellationToken cancellationToken = default);

    Task<Data.DTOs.Admin.AdminPayoutBatchDto?> MarkPaidAsync(
        int batchId,
        CancellationToken cancellationToken = default);

    Task<Data.DTOs.Admin.PagedResult<Data.DTOs.Admin.AdminPayoutBatchListItemDto>> ListBatchesAsync(
        Data.DTOs.Admin.AdminPayoutListFilter filter,
        CancellationToken cancellationToken = default);

    Task<Data.DTOs.Admin.AdminPayoutBatchDto?> GetBatchAsync(
        int batchId,
        CancellationToken cancellationToken = default);

    Task<Data.DTOs.Admin.PagedResult<Data.DTOs.Admin.AdminPendingEarningDto>> ListPendingEarningsAsync(
        Data.DTOs.Admin.AdminPendingEarningsFilter filter,
        CancellationToken cancellationToken = default);
}
