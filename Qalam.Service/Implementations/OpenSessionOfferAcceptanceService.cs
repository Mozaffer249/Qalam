using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qalam.Data.DTOs.OpenSessionRequests;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;
using Qalam.Service.Exceptions;

namespace Qalam.Service.Implementations;

public class OpenSessionOfferAcceptanceService : IOpenSessionOfferAcceptanceService
{
    private readonly ApplicationDBContext _db;
    private readonly ISessionAvailabilityMatchService _availabilityMatch;
    private readonly ICourseScheduleRepository _scheduleRepo;
    private readonly IFreeSessionPolicyService _freeSessionPolicy;
    private readonly EnrollmentSettings _enrollmentSettings;
    private readonly OpenSessionRequestSettings _osrSettings;

    public OpenSessionOfferAcceptanceService(
        ApplicationDBContext db,
        ISessionAvailabilityMatchService availabilityMatch,
        ICourseScheduleRepository scheduleRepo,
        IFreeSessionPolicyService freeSessionPolicy,
        IOptions<EnrollmentSettings> enrollmentSettings,
        IOptions<OpenSessionRequestSettings> osrSettings)
    {
        _db = db;
        _availabilityMatch = availabilityMatch;
        _scheduleRepo = scheduleRepo;
        _freeSessionPolicy = freeSessionPolicy;
        _enrollmentSettings = enrollmentSettings.Value;
        _osrSettings = osrSettings.Value;
    }

    public async Task<AcceptSessionOfferResultDto> AcceptAsync(
        int offerId,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var offer = await _db.OpenSessionOffers
            .Include(o => o.OpenSessionRequest)
                .ThenInclude(r => r.Sessions)
                    .ThenInclude(s => s.Units)
            .Include(o => o.OpenSessionRequest)
                .ThenInclude(r => r.Sessions)
                    .ThenInclude(s => s.TimeSlot)
            .Include(o => o.OpenSessionRequest)
                .ThenInclude(r => r.Invitations)
            .Include(o => o.OpenSessionRequest)
                .ThenInclude(r => r.Offers)
            .FirstOrDefaultAsync(o => o.Id == offerId, cancellationToken);

        if (offer == null)
            throw new InvalidOperationException("العرض غير موجود");

        var request = offer.OpenSessionRequest
            ?? throw new InvalidOperationException("طلب الجلسات غير موجود");

        if (request.Status is not (OpenSessionRequestStatus.Active or OpenSessionRequestStatus.ReceivingOffers))
            throw new InvalidOperationException($"لا يمكن قبول عرض على طلب في الحالة {request.Status}");

        if (offer.Status != OpenSessionOfferStatus.Pending)
            throw new InvalidOperationException("العرض ليس معلقاً");

        var now = DateTime.UtcNow;
        if (offer.ExpiresAt < now)
            throw new InvalidOperationException("انتهت صلاحية العرض");

        var sessions = request.Sessions.OrderBy(s => s.SequenceNumber).ToList();
        if (sessions.Count == 0)
            throw new InvalidOperationException("الطلب لا يحتوي على جلسات");

        var resolvedSlots = new List<(OpenSessionRequestSession Session, int TeacherAvailabilityId)>();
        foreach (var session in sessions)
        {
            if (!session.PreferredDate.HasValue || !session.TimeSlotId.HasValue)
                throw new InvalidOperationException(
                    $"الجلسة {session.SequenceNumber} تفتقد التاريخ أو الفترة الزمنية.");

            var dayOfWeekId = (int)session.PreferredDate.Value.DayOfWeek + 1;
            var availability = await _db.TeacherAvailabilities
                .AsNoTracking()
                .FirstOrDefaultAsync(ta =>
                        ta.TeacherId == offer.TeacherId
                        && ta.IsActive
                        && ta.TimeSlotId == session.TimeSlotId.Value
                        && ta.DayOfWeekId == dayOfWeekId,
                    cancellationToken);

            if (availability == null)
                throw new InvalidOperationException(
                    $"لا يوجد توافر أسبوعي للمعلم يطابق الجلسة {session.SequenceNumber} " +
                    $"({session.PreferredDate:yyyy-MM-dd}, timeSlotId={session.TimeSlotId}).");

            resolvedSlots.Add((session, availability.Id));
        }

        var match = await _availabilityMatch.MatchAsync(offer.TeacherId, request.Id, cancellationToken);
        var blocked = match.Where(m => m.Status != SessionAvailabilityStatus.Available).ToList();
        if (blocked.Count > 0)
            throw new SessionSlotConflictException(blocked);

        var firstSessionStartUtc = OpenSessionRequestExpiry.FirstSessionStartUtc(
            sessions.Select(s => (s.PreferredDate, s.TimeSlot != null ? (TimeSpan?)s.TimeSlot.StartTime : null)));

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            offer.Status = OpenSessionOfferStatus.Accepted;
            offer.AcceptedAt = now;
            offer.UpdatedAt = now;

            foreach (var sibling in request.Offers.Where(o =>
                         o.Id != offer.Id && o.Status == OpenSessionOfferStatus.Pending))
            {
                sibling.Status = OpenSessionOfferStatus.AutoRejected;
                sibling.RejectedAt = now;
                sibling.UpdatedAt = now;
            }

            var studentIds = new List<int> { request.StudentId };
            foreach (var inviteeId in request.Invitations
                         .Where(i => i.Status == OpenSessionRequestInvitationStatus.Accepted)
                         .Select(i => i.InvitedStudentId))
            {
                if (!studentIds.Contains(inviteeId))
                    studentIds.Add(inviteeId);
            }

            var isGroup = studentIds.Count > 1
                || request.GroupType is OfferGroupType.OpenGroup or OfferGroupType.InviteOnly;

            var applyFreeTrial = _freeSessionPolicy.IsEligiblePackage(isGroup, sessions.Count)
                && await _freeSessionPolicy.IsStudentEligibleForFreeTrialAsync(request.StudentId, cancellationToken);

            var preferredStart = sessions.Min(s => s.PreferredDate!.Value);
            var preferredEnd = sessions.Max(s => s.PreferredDate!.Value);
            var paymentDeadline = applyFreeTrial
                ? (DateTime?)null
                : OpenSessionRequestExpiry.ResolvePaymentDeadline(
                    now,
                    _enrollmentSettings.PaymentDeadlineHours,
                    firstSessionStartUtc,
                    _osrSettings,
                    isTargeted: request.TargetedTeacherId != null);

            var enrollment = new Enrollment
            {
                Source = EnrollmentSource.SessionRequest,
                CourseId = null,
                SessionRequestId = request.Id,
                SessionOfferId = offer.Id,
                Kind = isGroup ? EnrollmentKind.Group : EnrollmentKind.Individual,
                LeaderStudentId = isGroup ? request.StudentId : null,
                ApprovedByTeacherId = offer.TeacherId,
                ApprovedAt = now,
                PaymentDeadline = paymentDeadline,
                EnrollmentStatus = applyFreeTrial ? EnrollmentStatus.PendingPayment : EnrollmentStatus.PendingPayment,
                AmountDue = applyFreeTrial ? 0m : offer.Price,
                IsFreeTrial = applyFreeTrial,
                PricingSnapshotId = offer.PricingSnapshotId,
                OwnerUserId = request.RequestedByUserId,
                PreferredStartDate = preferredStart,
                PreferredEndDate = preferredEnd,
                Participants = studentIds.Select(sid => new EnrollmentParticipant
                {
                    StudentId = sid,
                    PaymentStatus = PaymentStatus.Pending
                }).ToList(),
                SelectedSessionSlots = resolvedSlots.Select(r => new EnrollmentSelectedSessionSlot
                {
                    SessionNumber = r.Session.SequenceNumber,
                    TeacherAvailabilityId = r.TeacherAvailabilityId,
                    SessionDate = r.Session.PreferredDate!.Value,
                    Units = r.Session.Units.Select(u => new EnrollmentSelectedSessionSlotUnit
                    {
                        ContentUnitId = u.ContentUnitId,
                        LessonId = u.LessonId
                    }).ToList()
                }).ToList()
            };

            _db.Enrollments.Add(enrollment);

            request.Status = OpenSessionRequestStatus.PaymentPending;
            request.UpdatedAt = now;

            if (applyFreeTrial)
            {
                var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
                if (student != null)
                {
                    student.HasUsedFreeTrialSession = true;
                    student.UpdatedAt = now;
                }

                // Platform bears teacher pay: student total 0; keep notional teacher earnings unless interview.
                if (offer.PricingSnapshotId.HasValue)
                {
                    var snapshot = await _db.PricingSnapshots
                        .FirstOrDefaultAsync(s => s.Id == offer.PricingSnapshotId.Value, cancellationToken);
                    if (snapshot != null)
                    {
                        var teacher = await _db.Teachers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == offer.TeacherId, cancellationToken);
                        var domainPricing = await _db.TeacherDomainPricings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                p => p.TeacherId == offer.TeacherId && p.DomainId == snapshot.DomainId,
                                cancellationToken);
                        var interviewPending = domainPricing is not
                            { HasCompletedInterviewSession: true, TeacherLevelId: not null }
                            && teacher is not { HasCompletedInterviewSession: true };
                        var notionalTeacherEarnings = snapshot.TeacherEarnings;
                        snapshot.TotalPrice = 0m;
                        if (interviewPending)
                        {
                            snapshot.TeacherSharePct = 0m;
                            snapshot.TeacherEarnings = 0m;
                            snapshot.PlatformShare = 0m;
                        }
                        else
                        {
                            // Platform cost = teacher earnings; student paid 0.
                            snapshot.PlatformShare = -notionalTeacherEarnings;
                        }
                        snapshot.UpdatedAt = now;
                    }
                }
            }

            var availabilityIds = resolvedSlots.Select(r => r.TeacherAvailabilityId).Distinct().ToList();
            var occupied = await _scheduleRepo.GetScheduledSlotsAsync(
                preferredStart, preferredEnd, availabilityIds, cancellationToken);
            foreach (var (session, availabilityId) in resolvedSlots)
            {
                if (!occupied.Contains((session.PreferredDate!.Value, availabilityId)))
                    continue;

                var conflictSessions = match
                    .Where(m => m.SequenceNumber == session.SequenceNumber)
                    .ToList();
                if (conflictSessions.Count == 0)
                {
                    conflictSessions =
                    [
                        new SessionAvailabilityMatchDto
                        {
                            SessionId = session.Id,
                            SequenceNumber = session.SequenceNumber,
                            PreferredDate = session.PreferredDate!.Value,
                            TimeSlotId = session.TimeSlotId ?? 0,
                            Status = SessionAvailabilityStatus.Conflict,
                        }
                    ];
                }

                throw new SessionSlotConflictException(conflictSessions);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var primaryParticipant = enrollment.Participants.First(p => p.StudentId == request.StudentId);

            return new AcceptSessionOfferResultDto
            {
                OfferId = offer.Id,
                EnrollmentId = enrollment.Id,
                ParticipantId = primaryParticipant.Id,
                AmountDue = enrollment.AmountDue,
                PaymentDeadline = enrollment.PaymentDeadline,
                RequestStatus = request.Status,
                IsFreeTrial = applyFreeTrial
            };
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
