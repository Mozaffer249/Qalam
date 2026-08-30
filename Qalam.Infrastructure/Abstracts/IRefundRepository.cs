using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Payment;

namespace Qalam.Infrastructure.Abstracts;

public interface IRefundRepository
{
    Task<(List<AdminRefundListItemDto> Items, int TotalCount)> ListAsync(
        AdminRefundListFilter filter,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetTrackedPaymentWithRefundsAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetRefundablePaymentIdsForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<List<EnrollmentPayment>> GetEnrollmentPaymentsForPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<RefundDetailProjection?> GetDetailProjectionAsync(
        int refundId,
        CancellationToken cancellationToken = default);

    Task<List<ScheduleStatusProjection>> GetScheduleStatusesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<List<EarningLineProjection>> GetEarningLinesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<List<TeacherEarningLine>> GetPendingEarningLinesForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);

    Task<int?> GetComplaintIdForRefundAsync(
        int refundId,
        CancellationToken cancellationToken = default);

    Task<int> GetTeacherIdForEnrollmentAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default);
}

public class RefundDetailProjection
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public int EnrollmentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "";
    public string? ProviderRefundId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? InitiatedByUserId { get; set; }
    public string? InitiatedByName { get; set; }
    public decimal PaymentTotal { get; set; }
    public decimal RefundedTotal { get; set; }
    public string? CourseTitle { get; set; }
    public string? PayerName { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int? ScheduleId { get; set; }
    public string? SessionLabel { get; set; }
    public string? PaymentProviderRef { get; set; }
}

public class ScheduleStatusProjection
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public DateOnly Date { get; set; }
}

public class EarningLineProjection
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public decimal Amount { get; set; }
    public string? BatchStatus { get; set; }
}
