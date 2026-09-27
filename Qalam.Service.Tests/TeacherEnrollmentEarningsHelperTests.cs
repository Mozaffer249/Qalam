using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Pricing;
using Qalam.Service.Mappers;

namespace Qalam.Service.Tests;

public class TeacherEnrollmentEarningsHelperTests
{
    [Fact]
    public void Compute_StudentFreeTrialWithoutInterview_TeacherPaidForAllSessions()
    {
        var enrollment = new Enrollment
        {
            IsFreeTrial = true,
            PricingSnapshot = new PricingSnapshot
            {
                TeacherEarnings = 140m,
                PlatformShare = -40m,
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
                    Id = 1,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Completed,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
                new CourseSchedule
                {
                    Id = 2,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Scheduled,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
            ],
        };

        var result = TeacherEnrollmentEarningsHelper.Compute(enrollment, []);
        Assert.Equal(0, result.FreeSessionsCount);
        Assert.Equal(2, result.PaidSessionsCount);
        Assert.Equal(140m, result.TeacherEarningsDue);
        Assert.Equal(0m, result.FreeSessionTeacherDeduction);
        Assert.Equal(70m, result.PerSessionTeacherValue);
    }

    [Fact]
    public void Compute_TeacherInterviewSchedule_CountsAsFreeWithDeduction()
    {
        var enrollment = new Enrollment
        {
            IsFreeTrial = false,
            PricingSnapshot = new PricingSnapshot
            {
                TeacherEarnings = 140m,
                PlatformShare = 60m,
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
                    Id = 10,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Completed,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
                new CourseSchedule
                {
                    Id = 11,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    DurationMinutes = 60,
                    Status = ScheduleStatus.Scheduled,
                    TeacherAvailabilityId = 1,
                    TeachingModeId = 1,
                },
            ],
        };

        var result = TeacherEnrollmentEarningsHelper.Compute(enrollment, [], teacherInterviewScheduleId: 10);
        Assert.Equal(1, result.FreeSessionsCount);
        Assert.Equal(1, result.PaidSessionsCount);
        Assert.Equal(140m, result.TeacherEarningsDue);
        Assert.Equal(70m, result.FreeSessionTeacherDeduction); // 140 × 60/120
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

    [Fact]
    public void Compute_LegacyZeroShareFreeTrial_ProjectsFullStarterShareEarnings()
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

        var result = TeacherEnrollmentEarningsHelper.Compute(enrollment, [], starterSharePct: 70m);
        Assert.True(result.IsInterviewPendingAtQuote);
        Assert.Equal(0m, result.TeacherEarningsDue);
        Assert.Equal(70m, result.ProjectedTeacherSharePct);
        Assert.Equal(119m, result.ProjectedTeacherEarningsDue);
        Assert.Equal(0m, result.ProjectedFreeSessionTeacherDeduction);
        Assert.Equal(59.5m, result.ProjectedPerSessionTeacherValue);
    }
}
