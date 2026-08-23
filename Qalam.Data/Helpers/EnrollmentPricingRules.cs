using Qalam.Data.Entity.Course;

namespace Qalam.Data.Helpers;

/// <summary>
/// Frozen enrollment pricing — never re-run live <see cref="PricingEngine"/> on read or pay.
/// Catalog/browse uses live estimates; enrollments use quotes captured at create time.
/// </summary>
public static class EnrollmentPricingRules
{
    /// <summary>
    /// Payable/display amount for an enrollment: snapshot → request estimate → amount due.
    /// </summary>
    public static decimal ResolvePayableAmount(Enrollment enrollment)
    {
        if (enrollment.PricingSnapshot is { TotalPrice: > 0 } snapshot)
            return snapshot.TotalPrice;

        if (enrollment.EnrollmentRequest is { EstimatedTotalPrice: > 0 } request)
            return request.EstimatedTotalPrice;

        return enrollment.AmountDue;
    }
}
