using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Entity.Teacher;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Infrastructure.Repositories;
using Qalam.Service.Abstracts;
using Qalam.Service.Exceptions;
using Qalam.Service.Implementations;

namespace Qalam.Service.Tests;

public class OsrSlotConflictGuardTests
{
    private static ApplicationDBContext CreateDb(string? databaseName = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EncryptionSettings:Key"] = "0123456789abcdef0123456789abcdef",
            })
            .Build();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDBContext(options, config);
    }

    [Fact]
    public async Task GetTeacherBookedSlotsInRangeAsync_includes_course_less_osr_enrollments()
    {
        await using var db = CreateDb();
        var teacherId = 42;
        var sessionDate = new DateOnly(2026, 9, 1);
        var start = new TimeSpan(10, 0, 0);
        var end = new TimeSpan(11, 0, 0);

        var timeSlot = new TimeSlot
        {
            Id = 1,
            StartTime = start,
            EndTime = end,
            DurationMinutes = 60,
        };
        var availability = new TeacherAvailability
        {
            Id = 5,
            TeacherId = teacherId,
            DayOfWeekId = 3,
            TimeSlotId = timeSlot.Id,
            TimeSlot = timeSlot,
            IsActive = true,
        };
        var osrEnrollment = new Enrollment
        {
            Id = 1,
            CourseId = null,
            ApprovedByTeacherId = teacherId,
            ApprovedAt = DateTime.UtcNow,
            EnrollmentStatus = EnrollmentStatus.Active,
            AmountDue = 100,
        };
        var schedule = new CourseSchedule
        {
            Id = 1,
            EnrollmentId = osrEnrollment.Id,
            Date = sessionDate,
            TeacherAvailabilityId = availability.Id,
            TeacherAvailability = availability,
            DurationMinutes = 60,
            TeachingModeId = 1,
            Status = ScheduleStatus.Scheduled,
            Enrollment = osrEnrollment,
        };

        db.TimeSlots.Add(timeSlot);
        db.TeacherAvailabilities.Add(availability);
        db.Enrollments.Add(osrEnrollment);
        db.CourseSchedules.Add(schedule);
        await db.SaveChangesAsync();

        var repo = new CourseScheduleRepository(db);
        var booked = await repo.GetTeacherBookedSlotsInRangeAsync(
            teacherId,
            sessionDate.AddDays(-1),
            sessionDate.AddDays(1));

        Assert.Single(booked);
        Assert.Equal(sessionDate, booked[0].Date);
        Assert.Equal(start, booked[0].Start);
        Assert.Equal(end, booked[0].End);
    }

    [Fact]
    public async Task AcceptAsync_throws_SessionSlotConflictException_when_match_blocked()
    {
        await using var db = CreateDb();
        var (offerId, _) = await SeedPendingOfferAsync(db);

        var blocked = new List<SessionAvailabilityMatchDto>
        {
            new()
            {
                SessionId = 1,
                SequenceNumber = 1,
                PreferredDate = new DateOnly(2026, 9, 1),
                TimeSlotId = 1,
                Status = SessionAvailabilityStatus.Conflict,
                ConflictWith = "existing booking",
            },
        };

        var match = new Mock<ISessionAvailabilityMatchService>();
        match.Setup(m => m.MatchAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blocked);

        var scheduleRepo = new Mock<ICourseScheduleRepository>();
        var sut = CreateAcceptanceService(db, match.Object, scheduleRepo.Object);

        var ex = await Assert.ThrowsAsync<SessionSlotConflictException>(
            () => sut.AcceptAsync(offerId, actingUserId: 99));

        Assert.Single(ex.BlockedSessions);
        Assert.Equal(SessionAvailabilityStatus.Conflict, ex.BlockedSessions[0].Status);
        Assert.Equal(0, await db.Enrollments.CountAsync());
        Assert.Equal(OpenSessionOfferStatus.Pending,
            await db.OpenSessionOffers.Where(o => o.Id == offerId).Select(o => o.Status).SingleAsync());
    }

    [Fact]
    public async Task AcceptAsync_throws_and_leaves_no_enrollment_when_scheduled_slot_occupied_in_tx()
    {
        await using var db = CreateDb();
        var sessionDate = new DateOnly(2026, 9, 1);
        var (offerId, availabilityId) = await SeedPendingOfferAsync(db);

        var available = new List<SessionAvailabilityMatchDto>
        {
            new()
            {
                SessionId = 1,
                SequenceNumber = 1,
                PreferredDate = sessionDate,
                TimeSlotId = 1,
                Status = SessionAvailabilityStatus.Available,
            },
        };

        var match = new Mock<ISessionAvailabilityMatchService>();
        match.Setup(m => m.MatchAsync(1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(available);

        var scheduleRepo = new Mock<ICourseScheduleRepository>();
        scheduleRepo
            .Setup(r => r.GetScheduledSlotsAsync(
                sessionDate,
                sessionDate,
                It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(availabilityId)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<(DateOnly Date, int TeacherAvailabilityId)>
            {
                (sessionDate, availabilityId),
            });

        var sut = CreateAcceptanceService(db, match.Object, scheduleRepo.Object);

        await Assert.ThrowsAsync<SessionSlotConflictException>(
            () => sut.AcceptAsync(offerId, actingUserId: 99));

        Assert.Equal(0, await db.Enrollments.CountAsync());
        Assert.Equal(OpenSessionOfferStatus.Pending,
            await db.OpenSessionOffers.Where(o => o.Id == offerId).Select(o => o.Status).SingleAsync());
    }

    [Fact]
    public async Task ReleaseAfterPaymentConflictAsync_restores_request_and_notifies_teacher()
    {
        await using var db = CreateDb();
        var now = DateTime.UtcNow;
        var sessionDate = new DateOnly(2026, 9, 1);

        var request = new OpenSessionRequest
        {
            Id = 1,
            StudentId = 1,
            RequestedByUserId = 1,
            DomainId = 1,
            Status = OpenSessionRequestStatus.PaymentPending,
            TargetedTeacherId = 1,
        };
        var acceptedOffer = new OpenSessionOffer
        {
            Id = 10,
            SessionRequestId = request.Id,
            TeacherId = 1,
            Price = 200,
            Status = OpenSessionOfferStatus.Accepted,
            ExpiresAt = now.AddDays(2),
            OpenSessionRequest = request,
        };
        var siblingOffer = new OpenSessionOffer
        {
            Id = 11,
            SessionRequestId = request.Id,
            TeacherId = 2,
            Price = 180,
            Status = OpenSessionOfferStatus.AutoRejected,
            RejectedAt = now,
            ExpiresAt = now.AddDays(2),
            OpenSessionRequest = request,
        };
        request.Offers = [acceptedOffer, siblingOffer];

        var enrollment = new Enrollment
        {
            Id = 50,
            CourseId = null,
            SessionRequestId = request.Id,
            SessionOfferId = acceptedOffer.Id,
            ApprovedByTeacherId = acceptedOffer.TeacherId,
            ApprovedAt = now,
            EnrollmentStatus = EnrollmentStatus.PendingPayment,
            AmountDue = acceptedOffer.Price,
            OpenSessionRequest = request,
            SelectedSessionSlots =
            [
                new EnrollmentSelectedSessionSlot
                {
                    SessionNumber = 1,
                    TeacherAvailabilityId = 5,
                    SessionDate = sessionDate,
                },
            ],
        };

        db.OpenSessionRequests.Add(request);
        db.OpenSessionOffers.AddRange(acceptedOffer, siblingOffer);
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync();

        string? capturedMessage = null;

        var convRepo = new Mock<IOfferConversationRepository>();
        convRepo
            .Setup(r => r.EnsureExistsAsync(request.Id, acceptedOffer.TeacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OfferConversation { Id = 77, SessionRequestId = request.Id, TeacherId = 1 });
        convRepo
            .Setup(r => r.AppendMessageAsync(
                It.IsAny<int>(),
                null,
                OfferMessageType.System,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int?, OfferMessageType, string, CancellationToken>(
                (_, _, _, content, _) => capturedMessage = content)
            .ReturnsAsync(new OfferMessage());

        var teacherRepo = new Mock<ITeacherRepository>();
        teacherRepo
            .Setup(r => r.GetEmailsByTeacherIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var rabbit = new Mock<IRabbitMQService>();
        var logger = new Mock<ILogger<OpenSessionRequestReleaseService>>();

        var sut = new OpenSessionRequestReleaseService(
            db,
            convRepo.Object,
            teacherRepo.Object,
            rabbit.Object,
            logger.Object);

        await sut.ReleaseAfterPaymentConflictAsync(enrollment.Id);

        var reloadedRequest = await db.OpenSessionRequests
            .Include(r => r.Offers)
            .SingleAsync(r => r.Id == request.Id);
        var reloadedEnrollment = await db.Enrollments.SingleAsync(e => e.Id == enrollment.Id);
        var reloadedAccepted = reloadedRequest.Offers.Single(o => o.Id == acceptedOffer.Id);
        var reloadedSibling = reloadedRequest.Offers.Single(o => o.Id == siblingOffer.Id);

        Assert.Equal(EnrollmentStatus.Cancelled, reloadedEnrollment.EnrollmentStatus);
        Assert.NotNull(reloadedEnrollment.CancelledAt);
        Assert.Equal(OpenSessionOfferStatus.Rejected, reloadedAccepted.Status);
        Assert.Equal(OpenSessionOfferStatus.Pending, reloadedSibling.Status);
        Assert.Null(reloadedSibling.RejectedAt);
        Assert.Equal(OpenSessionRequestStatus.ReceivingOffers, reloadedRequest.Status);

        convRepo.Verify(
            r => r.AppendMessageAsync(
                77,
                null,
                OfferMessageType.System,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.NotNull(capturedMessage);
        Assert.Contains("2026-09-01", capturedMessage);
    }

    private static OpenSessionOfferAcceptanceService CreateAcceptanceService(
        ApplicationDBContext db,
        ISessionAvailabilityMatchService match,
        ICourseScheduleRepository scheduleRepo)
    {
        return new OpenSessionOfferAcceptanceService(
            db,
            match,
            scheduleRepo,
            Options.Create(new EnrollmentSettings()),
            Options.Create(new OpenSessionRequestSettings()));
    }

    private static async Task<(int OfferId, int AvailabilityId)> SeedPendingOfferAsync(ApplicationDBContext db)
    {
        var sessionDate = new DateOnly(2026, 9, 1);
        var timeSlot = new TimeSlot
        {
            Id = 1,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            DurationMinutes = 60,
        };
        var availability = new TeacherAvailability
        {
            Id = 10,
            TeacherId = 1,
            DayOfWeekId = (int)sessionDate.DayOfWeek + 1,
            TimeSlotId = timeSlot.Id,
            TimeSlot = timeSlot,
            IsActive = true,
        };
        var session = new OpenSessionRequestSession
        {
            Id = 1,
            SequenceNumber = 1,
            PreferredDate = sessionDate,
            TimeSlotId = timeSlot.Id,
            TimeSlot = timeSlot,
            DurationMinutes = 60,
        };
        var request = new OpenSessionRequest
        {
            Id = 1,
            StudentId = 1,
            RequestedByUserId = 1,
            DomainId = 1,
            Status = OpenSessionRequestStatus.Active,
            Sessions = [session],
            Offers = [],
            Invitations = [],
        };
        session.SessionRequestId = request.Id;
        session.OpenSessionRequest = request;

        var offer = new OpenSessionOffer
        {
            Id = 1,
            SessionRequestId = request.Id,
            TeacherId = 1,
            Price = 150,
            Status = OpenSessionOfferStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            OpenSessionRequest = request,
        };
        request.Offers.Add(offer);

        db.TimeSlots.Add(timeSlot);
        db.TeacherAvailabilities.Add(availability);
        db.OpenSessionRequests.Add(request);
        db.OpenSessionOffers.Add(offer);
        await db.SaveChangesAsync();

        return (offer.Id, availability.Id);
    }
}
