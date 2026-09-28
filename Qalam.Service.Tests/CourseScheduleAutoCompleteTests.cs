using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;

namespace Qalam.Service.Tests;

public class CourseScheduleAutoCompleteTests
{
    private static ApplicationDBContext CreateDb()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDBContext(options, config);
    }

    private static async Task<CourseSchedule> SeedMidnightSlotScheduleAsync(ApplicationDBContext db, DateOnly date)
    {
        var slot = new TimeSlot
        {
            StartTime = new TimeSpan(23, 0, 0),
            EndTime = TimeSpan.Zero,
            DurationMinutes = 60,
            CreatedAt = DateTime.UtcNow,
        };
        db.Set<TimeSlot>().Add(slot);
        await db.SaveChangesAsync();

        var availability = new TeacherAvailability
        {
            TeacherId = 1,
            DayOfWeekId = 1,
            TimeSlotId = slot.Id,
            TimeSlot = slot,
            CreatedAt = DateTime.UtcNow,
        };
        db.Set<TeacherAvailability>().Add(availability);

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = 1,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        var schedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = date,
            DurationMinutes = 60,
            Status = ScheduleStatus.Scheduled,
            TeacherAvailabilityId = availability.Id,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.Add(schedule);
        await db.SaveChangesAsync();
        return schedule;
    }

    [Fact]
    public async Task GetOverdueForAutoComplete_SlotEndingAtMidnight_NotOverdueBeforeItEnds()
    {
        await using var db = CreateDb();
        var date = new DateOnly(2026, 9, 28);
        await SeedMidnightSlotScheduleAsync(db, date);
        var repo = new CourseScheduleRepository(db);

        var afternoon = PlatformTime.ToUtc(date, new TimeSpan(17, 0, 0));
        var overdue = await repo.GetOverdueForAutoCompleteAsync(afternoon);

        Assert.Empty(overdue);
    }

    [Fact]
    public async Task GetOverdueForAutoComplete_SlotEndingAtMidnight_OverdueAfterMidnight()
    {
        await using var db = CreateDb();
        var date = new DateOnly(2026, 9, 28);
        var schedule = await SeedMidnightSlotScheduleAsync(db, date);
        var repo = new CourseScheduleRepository(db);

        var afterMidnight = PlatformTime.ToUtc(date.AddDays(1), new TimeSpan(0, 5, 0));
        var overdue = await repo.GetOverdueForAutoCompleteAsync(afterMidnight);

        Assert.Contains(overdue, s => s.Id == schedule.Id);
    }
}
