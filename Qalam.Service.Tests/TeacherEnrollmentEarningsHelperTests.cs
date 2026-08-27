using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Mappers;

namespace Qalam.Service.Tests;

public class TeacherEnrollmentEarningsHelperTests
{
    [Fact]
    public void Compute_FreeTrial_SetsFreePaidCountsAndDeduction()
    {
        var enrollment = new Enrollment
        {
            IsFreeTrial = true,
            PricingSnapshot = new PricingSnapshot
            {
                TeacherEarnings = 70m,
                PlatformShare = 30m,
                TeacherSharePct = 70m,
                TotalMinutes = 120,
                PricePerHour = 100m,
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
                    Status = ScheduleStatus.Completed,
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

        var result = TeacherEnrollmentEarningsHelper.Compute(enrollment, []);
        Assert.Equal(1, result.FreeSessionsCount);
        Assert.Equal(1, result.PaidSessionsCount);
        Assert.Equal(70m, result.TeacherEarningsDue);
        Assert.Equal(70m, result.FreeSessionTeacherDeduction); // 70 * 60/60 earnable
        Assert.Equal("Pending", result.EarningUiStatus);
    }

    [Fact]
    public void ResolveUiStatus_AvailableWhenPendingLineExists()
    {
        var status = TeacherEnrollmentEarningsHelper.ResolveUiStatus(
        [
            new TeacherEnrollmentEarningsHelper.EarningLineInfo(
                TeacherEarningLineStatus.Pending, null, 40m),
        ]);
        Assert.Equal("Available", status);
    }

    [Fact]
    public void ResolveUiStatus_PaidWhenIncludedAndBatchPaid()
    {
        var status = TeacherEnrollmentEarningsHelper.ResolveUiStatus(
        [
            new TeacherEnrollmentEarningsHelper.EarningLineInfo(
                TeacherEarningLineStatus.IncludedInPayout, PayoutBatchStatus.Paid, 40m),
        ]);
        Assert.Equal("Paid", status);
    }
}
