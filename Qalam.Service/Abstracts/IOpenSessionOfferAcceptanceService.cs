using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface IOpenSessionOfferAcceptanceService
{
    /// <summary>
    /// Accepts a pending offer: marks it Accepted, auto-rejects siblings, moves the request to
    /// PaymentPending, and creates a course-less PendingPayment Enrollment with selected slots
    /// resolved to the teacher's TeacherAvailability rows.
    /// </summary>
    /// <exception cref="InvalidOperationException">Business-rule failure (caller maps to 400).</exception>
    Task<AcceptSessionOfferResultDto> AcceptAsync(int offerId, int actingUserId, CancellationToken cancellationToken = default);
}
