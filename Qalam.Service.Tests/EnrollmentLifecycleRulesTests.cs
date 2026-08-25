using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Service.Helpers;

namespace Qalam.Service.Tests;

public class EnrollmentLifecycleRulesTests
{
    [Fact]
    public void CanStudentCancel_PendingPayment_Allows()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.PendingPayment,
            CourseSchedules = new List<CourseSchedule>()
        };

        Assert.True(EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: true));
        Assert.False(EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: false));
    }

    [Fact]
    public void CanStudentCancel_ActiveBeforeFirstSession_Allows()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.Active,
            CourseSchedules = new List<CourseSchedule>
            {
                new() { Status = ScheduleStatus.Scheduled, Attendances = new List<SessionAttendance>() }
            }
        };

        Assert.True(EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: true));
    }

    [Fact]
    public void CanStudentCancel_ActiveAfterCompletedSession_Blocks()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.Active,
            CourseSchedules = new List<CourseSchedule>
            {
                new() { Status = ScheduleStatus.Completed, Attendances = new List<SessionAttendance>() }
            }
        };

        Assert.False(EnrollmentLifecycleRules.CanStudentCancel(enrollment, isOwner: true));
        Assert.True(EnrollmentLifecycleRules.HasSessionStarted(enrollment));
    }

    [Fact]
    public void ShouldMarkEnrollmentCompleted_LastOfN_Completes()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.Active,
            CourseSchedules = new List<CourseSchedule>
            {
                new() { Status = ScheduleStatus.Completed },
                new() { Status = ScheduleStatus.Completed },
                new() { Status = ScheduleStatus.Cancelled },
            }
        };

        Assert.True(EnrollmentLifecycleRules.ShouldMarkEnrollmentCompleted(enrollment));
    }

    [Fact]
    public void ShouldMarkEnrollmentCompleted_OneStillScheduled_StaysActive()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.Active,
            CourseSchedules = new List<CourseSchedule>
            {
                new() { Status = ScheduleStatus.Completed },
                new() { Status = ScheduleStatus.Scheduled },
            }
        };

        Assert.False(EnrollmentLifecycleRules.ShouldMarkEnrollmentCompleted(enrollment));
    }

    [Fact]
    public void ShouldMarkEnrollmentCompleted_CancelledOnly_DoesNotComplete()
    {
        var enrollment = new Enrollment
        {
            EnrollmentStatus = EnrollmentStatus.Active,
            CourseSchedules = new List<CourseSchedule>
            {
                new() { Status = ScheduleStatus.Cancelled },
            }
        };

        Assert.False(EnrollmentLifecycleRules.ShouldMarkEnrollmentCompleted(enrollment));
    }
}
