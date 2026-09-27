using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Mappers;

namespace Qalam.Service.Tests;

public class EnrollmentEarningsProjectionHelperTests
{
    private static Enrollment LegacyZeroShareEnrollment(bool isFreeTrial, decimal amountDue) => new()
    {
        IsFreeTrial = isFreeTrial,
        AmountDue = amountDue,
        PricingSnapshot = new PricingSnapshot
        {
            TeacherEarnings = 0m,
            PlatformShare = amountDue,
            TeacherSharePct = 0m,
            TotalMinutes = 120,
            TotalPrice = amountDue,
            PricePerHour = 85m,
            Currency = "SAR",
            MarketCode = "SA",
            SessionTypeCode = "individual",
        },
        CourseSchedules =
        [
            new CourseSchedule
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                DurationMinutes = 60,
                Status = ScheduleStatus.Scheduled,
                TeacherAvailabilityId = 1,
                TeachingModeId = 1,
            },
            new CourseSchedule
            {
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                DurationMinutes = 60,
                Status = ScheduleStatus.Scheduled,
                TeacherAvailabilityId = 1,
                TeachingModeId = 1,
            },
        ],
    };

    [Fact]
    public void Compute_LegacyZeroShareFreeTrial_ProjectsFullPackage_PlatformCoversCredit()
    {
        var enrollment = LegacyZeroShareEnrollment(isFreeTrial: true, amountDue: 85m);

        var projection = EnrollmentEarningsProjectionHelper.Compute(enrollment, starterSharePct: 70m);
        Assert.NotNull(projection);
        Assert.True(projection!.IsInterviewPendingAtQuote);
        Assert.Equal(119m, projection.ProjectedTeacherEarningsDue);
        Assert.Equal(0m, projection.ProjectedFreeSessionTeacherDeduction);
        Assert.Equal(59.5m, projection.ProjectedPerSessionTeacherValue);
        Assert.Equal(-34m, projection.ProjectedPlatformShare);
    }

    [Fact]
    public void Compute_LegacyZeroSharePaidEnrollment_ProjectsFullPackage()
    {
        var enrollment = LegacyZeroShareEnrollment(isFreeTrial: false, amountDue: 170m);

        var projection = EnrollmentEarningsProjectionHelper.Compute(enrollment, starterSharePct: 70m);
        Assert.NotNull(projection);
        Assert.Equal(119m, projection!.ProjectedTeacherEarningsDue);
        Assert.Equal(51m, projection.ProjectedPlatformShare);
    }

    [Fact]
    public void Compute_SnapshotWithShare_ReturnsNull()
    {
        var enrollment = LegacyZeroShareEnrollment(isFreeTrial: true, amountDue: 85m);
        enrollment.PricingSnapshot!.TeacherSharePct = 70m;

        Assert.Null(EnrollmentEarningsProjectionHelper.Compute(enrollment, starterSharePct: 70m));
    }
}
