using Qalam.Data.DTOs.Policy;

namespace Qalam.Service.Abstracts;

public interface IPolicyCaseAdminService
{
    Task<PolicyCaseListResultDto> ListAsync(PolicyCaseListFilter filter, CancellationToken cancellationToken = default);

    Task<PolicyCaseDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a manual admin exception as a new AdminException case. Validated against the current policy's
    /// AdminExceptions rules (allowed actions, minimum reason length, SuperAdmin above the admin amount limit).
    /// </summary>
    /// <exception cref="Models.Policy.PolicyDeniedException">The policy does not allow it.</exception>
    /// <exception cref="InvalidOperationException">Invalid input (amount, schedule, slot).</exception>
    Task<PolicyCaseDetailDto> ApplyExceptionAsync(
        AdminPolicyExceptionRequest request,
        int adminUserId,
        bool isSuperAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes compensating entries for a case (wallet debits, negative refund rows, teacher corrections,
    /// re-accrued earnings) as a new Reversal case and marks the original Reversed. Nothing is deleted.
    /// Refunds sent to the original payment method cannot be reversed.
    /// </summary>
    /// <returns>Null when the case does not exist.</returns>
    Task<PolicyCaseDetailDto?> ReverseAsync(int id, string reason, int adminUserId, CancellationToken cancellationToken = default);

    /// <returns>Null when the enrollment does not exist.</returns>
    Task<List<FinancialTimelineEntryDto>?> GetEnrollmentTimelineAsync(int enrollmentId, CancellationToken cancellationToken = default);
}
