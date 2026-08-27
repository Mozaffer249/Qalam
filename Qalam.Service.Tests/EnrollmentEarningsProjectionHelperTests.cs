using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Mappers;

namespace Qalam.Service.Tests;

public class EnrollmentEarningsProjectionHelperTests
{
    [Fact]
    public void Compute_InterviewPendingFreeTrial_MatchesEnrollment3022Shape()
    {
        var enrollment = new Enrollment
        {
            IsFreeTrial = true,
            AmountDue = 85m,
            PricingSnapshot = new PricingSnapshot
            {
                TeacherEarnings = 0m,
                PlatformShare = 85m,
                TeacherSharePct = 0m,
                TotalMinutes = 120,
                TotalPrice = 85m,
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

        var projection = EnrollmentEarningsProjectionHelper.Compute(enrollment, starterSharePct: 70m);
        Assert.NotNull(projection);
        Assert.True(projection!.IsInterviewPendingAtQuote);
        Assert.Equal(59.5m, projection.ProjectedTeacherEarningsDue);
        Assert.Equal(59.5m, projection.ProjectedFreeSessionTeacherDeduction);
        Assert.Equal(25.5m, projection.ProjectedPlatformShare);
        Assert.Equal(85m, enrollment.PricingSnapshot.TotalPrice);
    }

    [Fact]
    public void ResolvePackageEarningsForAccrual_UsesProjectionWhenSnapshotZero()
    {
        var enrollment = new Enrollment
        {
            IsFreeTrial = true,
            AmountDue = 85m,
            PricingSnapshot = new PricingSnapshot
            {
                TeacherEarnings = 0m,
                TeacherSharePct = 0m,
                TotalMinutes = 120,
                PricePerHour = 85m,
                Currency = "SAR",
                MarketCode = "SA",
                SessionTypeCode = "individual",
            },
            CourseSchedules =
            [
                new CourseSchedule
                {
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Scheduled,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
                new CourseSchedule
                {
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Scheduled,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
            ],
        };

        var package = EnrollmentEarningsProjectionHelper.ResolvePackageEarningsForAccrual(
            enrollment, enrollment.PricingSnapshot, starterSharePct: 70m);
        Assert.Equal(59.5m, package);
    }
}
