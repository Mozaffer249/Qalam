using Qalam.Data.Entity.Course;

namespace Qalam.Service.Abstracts;

public interface IEnrollmentApprovalService
{
    /// <summary>
    /// Creates the post-approval enrollment artifact (a single <see cref="Enrollment"/>
    /// with one or more <see cref="EnrollmentParticipant"/>s) for an Approved request.
    /// The enrollment lands in <see cref="Qalam.Data.Entity.Common.Enums.EnrollmentStatus.PendingPayment"/>.
    /// Caller owns the transaction and is responsible for flipping
    /// <c>request.Status</c> to <see cref="Qalam.Data.Entity.Common.Enums.RequestStatus.Approved"/>.
    /// </summary>
    Task<Enrollment> CreatePendingPaymentArtifactsAsync(
        CourseEnrollmentRequest request,
        Course course,
        int approvingTeacherId,
        DateTime paymentDeadline,
        CancellationToken cancellationToken);
}
