using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;

namespace Qalam.Service.Payments;

/// <summary>Checks shared by every "pay this enrollment now" path (mock/free, wallet).</summary>
public static class EnrollmentPayabilityRules
{
    /// <summary>Returns an error message, or null when <paramref name="userId"/> may pay now.</summary>
    public static string? Validate(Enrollment enrollment, int userId, DateTime nowUtc)
    {
        if (enrollment.EnrollmentStatus != EnrollmentStatus.PendingPayment)
            return "Only pending-payment enrollments can be paid.";

        if (enrollment.PaymentDeadline.HasValue && enrollment.PaymentDeadline.Value < nowUtc)
            return "Payment deadline has expired.";

        if (enrollment.EnrollmentRequest == null
            && (enrollment.SelectedSessionSlots == null || enrollment.SelectedSessionSlots.Count == 0))
            return "Enrollment is missing schedule selections — cannot generate schedules.";

        var ownerUserId = enrollment.EnrollmentRequest?.RequestedByUserId ?? enrollment.OwnerUserId;
        if (!ownerUserId.HasValue || ownerUserId.Value != userId)
            return "Only the enrollment owner can pay for this enrollment.";

        if (enrollment.PaidByUserId.HasValue
            || enrollment.Participants.Any(p => p.PaymentStatus == PaymentStatus.Succeeded))
            return "This enrollment has already been paid.";

        return null;
    }

    public static string DescribeEnrollment(Enrollment enrollment)
    {
        var isSessionRequest = enrollment.Source == EnrollmentSource.SessionRequest
                               || enrollment.CourseId == null;
        return isSessionRequest
            ? (enrollment.OpenSessionRequest?.Subject?.NameEn
               ?? enrollment.OpenSessionRequest?.Subject?.NameAr
               ?? "Session request enrollment")
            : enrollment.Course?.Title ?? $"Enrollment #{enrollment.Id}";
    }
}
