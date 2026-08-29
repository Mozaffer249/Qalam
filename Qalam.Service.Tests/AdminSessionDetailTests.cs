using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.Admin;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;

namespace Qalam.Service.Tests;

public class AdminSessionDetailTests
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

    [Fact]
    public async Task ListAsync_IncludesComplaintFlagsAndAuditDataExists()
    {
        await using var db = CreateDb();

        var enrollment = new Enrollment
        {
            ApprovedByTeacherId = 5,
            ApprovedAt = DateTime.UtcNow,
            Kind = EnrollmentKind.Individual,
            EnrollmentStatus = EnrollmentStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        db.EnrollmentParticipants.Add(new EnrollmentParticipant
        {
            EnrollmentId = enrollment.Id,
            StudentId = 7,
            PaymentStatus = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow,
        });

        var schedule = new CourseSchedule
        {
            EnrollmentId = enrollment.Id,
            Date = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DurationMinutes = 60,
            Status = ScheduleStatus.Completed,
            TeacherAttendanceStatus = SessionAttendanceStatus.Present,
            TeacherAvailabilityId = 1,
            TeachingModeId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        db.CourseSchedules.Add(schedule);
        await db.SaveChangesAsync();

        db.SessionComplaints.Add(new SessionComplaint
        {
            CourseScheduleId = schedule.Id,
            EnrollmentId = enrollment.Id,
            StudentId = 7,
            TeacherId = 5,
            ReasonCode = SessionComplaintReason.TechnicalIssue,
            Description = "Audio dropped",
            Status = SessionComplaintStatus.Open,
            FiledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        });

        db.SessionAuditLogs.Add(new SessionAuditLog
        {
            CourseScheduleId = schedule.Id,
            ActorUserId = 1,
            ActorRole = "Admin",
            ActionType = SessionAuditActionType.ComplaintFiled,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var sut = new AdminSessionReadRepository(
            db,
            Options.Create(new LiveSessionSettings { LiveKit = new LiveKitProviderSettings { Url = "wss://live.example" } }));

        var list = await sut.ListAsync(new AdminSessionListFilter { EnrollmentId = enrollment.Id });

        var item = Assert.Single(list);
        Assert.Equal(schedule.Id, item.ScheduleId);
        Assert.True(item.HasOpenComplaint);
        Assert.Equal(1, item.ComplaintCount);

        var audit = Assert.Single(db.SessionAuditLogs.Where(l => l.CourseScheduleId == schedule.Id).ToList());
        Assert.Equal(SessionAuditActionType.ComplaintFiled, audit.ActionType);
    }
}
