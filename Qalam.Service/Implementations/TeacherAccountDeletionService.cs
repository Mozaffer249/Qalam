using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Course;
using Qalam.Data.Entity.Identity;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Data.Entity.Payment;
using Qalam.Data.Entity.Session;
using Qalam.Data.Entity.Teacher;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class TeacherAccountDeletionService : ITeacherAccountDeletionService
{
    private readonly ApplicationDBContext _db;
    private readonly UserManager<User> _userManager;
    private readonly ISecurityNotificationService _securityNotification;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<TeacherAccountDeletionService> _logger;

    public TeacherAccountDeletionService(
        ApplicationDBContext db,
        UserManager<User> userManager,
        ISecurityNotificationService securityNotification,
        IFileStorageService fileStorage,
        ILogger<TeacherAccountDeletionService> logger)
    {
        _db = db;
        _userManager = userManager;
        _securityNotification = securityNotification;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> DeleteTeacherAccountAsync(
        int teacherId,
        int adminId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var teacher = await _db.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);
        if (teacher == null)
            return (false, "Teacher not found");

        if (!teacher.UserId.HasValue)
            return (false, "Teacher has no linked user account");

        var userId = teacher.UserId.Value;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "Linked user account not found");

        var filePaths = await CollectFilePathsAsync(teacherId, user, cancellationToken);

        try
        {
            await _securityNotification.NotifyAccountDeletedAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Account-deleted notification failed for teacher {TeacherId}; continuing delete",
                teacherId);
        }

        foreach (var path in filePaths)
        {
            try
            {
                await _fileStorage.DeleteFileAsync(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Best-effort file delete failed for {Path}", path);
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await WipeTeacherOwnedGraphAsync(teacherId, cancellationToken);

            // Shadow FK from duplicate TeacherSubjects relationship (TeacherId1) — clear before teacher row goes.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE education.TeacherSubjects SET TeacherId1 = NULL WHERE TeacherId1 = {teacherId}",
                cancellationToken);

            await _db.Teachers
                .Where(t => t.Id == teacherId)
                .ExecuteDeleteAsync(cancellationToken);

            // Same Identity user may also have Student/Guardian (dual-role / bad data) — wipe so User can go.
            await WipeLinkedStudentGuardianAsync(userId, cancellationToken);

            // Restrict FKs to User that Identity cascade cannot clear.
            await _db.EnrollmentConversationMessages
                .Where(m => m.SenderUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OfferMessages
                .Where(m => m.SenderUserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            // Enrollments that still point at this user (should be rare for teachers).
            await _db.Enrollments
                .Where(e => e.PaidByUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.PaidByUserId, (int?)null), cancellationToken);
            await _db.Enrollments
                .Where(e => e.OwnerUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.OwnerUserId, (int?)null), cancellationToken);
            await _db.Enrollments
                .Where(e => e.CancelledByUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.CancelledByUserId, (int?)null), cancellationToken);

            var payerPaymentIds = await _db.Payments
                .Where(p => p.PayerUserId == userId)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            if (payerPaymentIds.Count > 0)
            {
                await _db.EnrollmentPayments
                    .Where(ep => payerPaymentIds.Contains(ep.PaymentId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.PaymentItems
                    .Where(pi => payerPaymentIds.Contains(pi.PaymentId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.Payments
                    .Where(p => payerPaymentIds.Contains(p.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.LoginOtps.Where(o => o.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.PhoneConfirmationOtps.Where(o => o.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.AuditLogs.Where(a => a.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.SecurityEvents.Where(e => e.UserId == userId).ExecuteDeleteAsync(cancellationToken);
            await _db.IpLoginAttempts.Where(a => a.UserId == userId).ExecuteDeleteAsync(cancellationToken);

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to delete user: {errors}");
            }

            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Teacher {TeacherId} and user {UserId} hard-deleted by admin {AdminId}. Reason: {Reason}",
                teacherId,
                userId,
                adminId,
                reason);

            return (true, "Teacher account and related data deleted successfully");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Hard-delete failed for teacher {TeacherId}", teacherId);
            return (false, FormatDbError(ex));
        }
    }

    private static string FormatDbError(Exception ex)
    {
        var inner = ex;
        while (inner.InnerException != null)
            inner = inner.InnerException;
        var detail = inner.Message;
        if (string.IsNullOrWhiteSpace(detail) || detail == ex.Message)
            return ex.Message;
        return $"{ex.Message} ({detail})";
    }

    /// <summary>
    /// Removes Student/Guardian profiles on the same UserId so Identity User delete can succeed.
    /// </summary>
    private async Task WipeLinkedStudentGuardianAsync(int userId, CancellationToken cancellationToken)
    {
        var studentIds = await _db.Students
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentIds)
            await WipeStudentGraphAsync(studentId, cancellationToken);

        // Detach guardian login link; delete orphan guardians with no remaining students.
        await _db.Guardians
            .Where(g => g.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.UserId, (int?)null), cancellationToken);

        await _db.Guardians
            .Where(g => g.UserId == null && !_db.Students.Any(s => s.GuardianId == g.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task WipeStudentGraphAsync(int studentId, CancellationToken cancellationToken)
    {
        await _db.TeacherReviews
            .Where(r => r.StudentId == studentId)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.SessionAttendances
            .Where(a => a.StudentId == studentId)
            .ExecuteDeleteAsync(cancellationToken);

        await _db.CourseRequestGroupMembers
            .Where(m => m.StudentId == studentId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.CourseRequestGroupMembers
            .Where(m => m.InvitedByStudentId == studentId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.InvitedByStudentId, (int?)null),
                cancellationToken);

        var participantIds = await _db.EnrollmentParticipants
            .Where(p => p.StudentId == studentId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        if (participantIds.Count > 0)
        {
            await _db.EnrollmentPayments
                .Where(ep => participantIds.Contains(ep.EnrollmentParticipantId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.EnrollmentParticipants
                .Where(p => participantIds.Contains(p.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.Enrollments
            .Where(e => e.LeaderStudentId == studentId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.LeaderStudentId, (int?)null),
                cancellationToken);

        // Open session requests owned by this student
        var osrIds = await _db.OpenSessionRequests
            .Where(r => r.StudentId == studentId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (osrIds.Count > 0)
        {
            var osrEnrollmentIds = await _db.Enrollments
                .Where(e => e.SessionRequestId != null && osrIds.Contains(e.SessionRequestId.Value))
                .Select(e => e.Id)
                .ToListAsync(cancellationToken);
            if (osrEnrollmentIds.Count > 0)
                await WipeEnrollmentIdsAsync(osrEnrollmentIds, cancellationToken);

            var offerIds = await _db.OpenSessionOffers
                .Where(o => osrIds.Contains(o.SessionRequestId))
                .Select(o => o.Id)
                .ToListAsync(cancellationToken);
            if (offerIds.Count > 0)
            {
                var offerConvIds = await _db.OfferConversations
                    .Where(c => c.SessionOfferId != null && offerIds.Contains(c.SessionOfferId.Value))
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                // Also conversations for this request even without offer
                var reqConvIds = await _db.OfferConversations
                    .Where(c => osrIds.Contains(c.SessionRequestId))
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken);
                var allConvIds = offerConvIds.Union(reqConvIds).Distinct().ToList();
                if (allConvIds.Count > 0)
                {
                    await _db.OfferMessages
                        .Where(m => allConvIds.Contains(m.OfferConversationId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await _db.OfferConversations
                        .Where(c => allConvIds.Contains(c.Id))
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(c => c.SessionOfferId, (int?)null),
                            cancellationToken);
                    await _db.OfferConversations
                        .Where(c => allConvIds.Contains(c.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _db.OpenSessionOffers
                    .Where(o => offerIds.Contains(o.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.OpenSessionRequestInvitations
                .Where(i => osrIds.Contains(i.SessionRequestId)
                    || i.InvitedStudentId == studentId
                    || i.InvitedByStudentId == studentId)
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OpenSessionRequestTargets
                .Where(t => osrIds.Contains(t.SessionRequestId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OpenSessionRequestSessionUnits
                .Where(u => _db.OpenSessionRequestSessions.Any(s =>
                    s.Id == u.SessionRequestSessionId && osrIds.Contains(s.SessionRequestId)))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OpenSessionRequestSessions
                .Where(s => osrIds.Contains(s.SessionRequestId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OpenSessionRequestAttachments
                .Where(a => osrIds.Contains(a.SessionRequestId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.OpenSessionRequests
                .Where(r => osrIds.Contains(r.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // Invitations on other requests
        await _db.OpenSessionRequestInvitations
            .Where(i => i.InvitedStudentId == studentId || i.InvitedByStudentId == studentId)
            .ExecuteDeleteAsync(cancellationToken);

        // Legacy session schema
        var legacySessionIds = await _db.Sessions
            .Where(s => s.StudentId == studentId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (legacySessionIds.Count > 0)
        {
            await _db.ScheduledSessions
                .Where(s => legacySessionIds.Contains(s.SessionId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Sessions
                .Where(s => legacySessionIds.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var legacyRequestIds = await _db.SessionRequests
            .Where(r => r.StudentId == studentId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (legacyRequestIds.Count > 0)
        {
            await _db.SessionRequestOffers
                .Where(o => legacyRequestIds.Contains(o.SessionRequestId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.SessionRequests
                .Where(r => legacyRequestIds.Contains(r.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.Students
            .Where(s => s.Id == studentId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task WipeEnrollmentIdsAsync(List<int> enrollmentIds, CancellationToken cancellationToken)
    {
        if (enrollmentIds.Count == 0) return;

        var participantIds = await _db.EnrollmentParticipants
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);
        if (participantIds.Count > 0)
        {
            await _db.EnrollmentPayments
                .Where(ep => participantIds.Contains(ep.EnrollmentParticipantId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var conversationIds = await _db.EnrollmentConversations
            .Where(c => enrollmentIds.Contains(c.EnrollmentId))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (conversationIds.Count > 0)
        {
            await _db.EnrollmentConversationMessages
                .Where(m => conversationIds.Contains(m.EnrollmentConversationId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.EnrollmentConversations
                .Where(c => conversationIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var slotIds = await _db.EnrollmentSelectedSessionSlots
            .Where(s => enrollmentIds.Contains(s.EnrollmentId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (slotIds.Count > 0)
        {
            await _db.EnrollmentSelectedSessionSlotUnits
                .Where(u => slotIds.Contains(u.EnrollmentSelectedSessionSlotId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.EnrollmentSelectedSessionSlots
                .Where(s => slotIds.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var scheduleIds = await _db.CourseSchedules
            .Where(s => enrollmentIds.Contains(s.EnrollmentId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (scheduleIds.Count > 0)
        {
            await _db.SessionAttendances
                .Where(a => scheduleIds.Contains(a.CourseScheduleId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.SessionLivePresenceEvents
                .Where(e => scheduleIds.Contains(e.CourseScheduleId))
                .ExecuteDeleteAsync(cancellationToken);
            var homeworkIds = await _db.SessionHomeworkAssignments
                .Where(h => scheduleIds.Contains(h.CourseScheduleId))
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);
            if (homeworkIds.Count > 0)
            {
                await _db.SessionHomeworkFileLinks
                    .Where(l => homeworkIds.Contains(l.SessionHomeworkAssignmentId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.SessionHomeworkAssignments
                    .Where(h => homeworkIds.Contains(h.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            await _db.SessionContentLinks
                .Where(l => scheduleIds.Contains(l.CourseScheduleId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.CourseSchedules
                .Where(s => scheduleIds.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.EnrollmentParticipants
            .Where(p => enrollmentIds.Contains(p.EnrollmentId))
            .ExecuteDeleteAsync(cancellationToken);
        await _db.Enrollments
            .Where(e => enrollmentIds.Contains(e.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<List<string>> CollectFilePathsAsync(
        int teacherId,
        User user,
        CancellationToken cancellationToken)
    {
        var paths = new List<string>();

        var docPaths = await _db.TeacherDocuments
            .AsNoTracking()
            .Where(d => d.TeacherId == teacherId && d.FilePath != null && d.FilePath != "")
            .Select(d => d.FilePath!)
            .ToListAsync(cancellationToken);
        paths.AddRange(docPaths);

        var contentUrls = await _db.TeacherContentItems
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .Select(c => new { c.PublicUrl, c.StorageKey })
            .ToListAsync(cancellationToken);
        foreach (var c in contentUrls)
        {
            if (!string.IsNullOrWhiteSpace(c.PublicUrl)) paths.Add(c.PublicUrl);
            else if (!string.IsNullOrWhiteSpace(c.StorageKey)) paths.Add(c.StorageKey);
        }

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
            paths.Add(user.ProfilePictureUrl);

        return paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task WipeTeacherOwnedGraphAsync(int teacherId, CancellationToken cancellationToken)
    {
        var courseIds = await _db.Courses
            .Where(c => c.TeacherId == teacherId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var offerIds = await _db.OpenSessionOffers
            .Where(o => o.TeacherId == teacherId)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var enrollmentIds = await _db.Enrollments
            .Where(e =>
                (e.CourseId != null && courseIds.Contains(e.CourseId.Value))
                || (e.CourseId == null && e.ApprovedByTeacherId == teacherId)
                || (e.SessionOfferId != null && offerIds.Contains(e.SessionOfferId.Value)))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var availIds = await _db.TeacherAvailabilities
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var contentItemIds = await _db.TeacherContentItems
            .Where(c => c.TeacherId == teacherId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        // --- Payments for enrollments being deleted ---
        if (enrollmentIds.Count > 0)
        {
            var participantIds = await _db.EnrollmentParticipants
                .Where(p => enrollmentIds.Contains(p.EnrollmentId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (participantIds.Count > 0)
            {
                var paymentIds = await _db.EnrollmentPayments
                    .Where(ep => participantIds.Contains(ep.EnrollmentParticipantId))
                    .Select(ep => ep.PaymentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                await _db.EnrollmentPayments
                    .Where(ep => participantIds.Contains(ep.EnrollmentParticipantId))
                    .ExecuteDeleteAsync(cancellationToken);

                if (paymentIds.Count > 0)
                {
                    await _db.PaymentItems
                        .Where(pi => paymentIds.Contains(pi.PaymentId)
                            && !_db.EnrollmentPayments.Any(ep => ep.PaymentId == pi.PaymentId))
                        .ExecuteDeleteAsync(cancellationToken);

                    await _db.Payments
                        .Where(p => paymentIds.Contains(p.Id)
                            && !_db.EnrollmentPayments.Any(ep => ep.PaymentId == p.Id)
                            && !_db.PaymentItems.Any(pi => pi.PaymentId == p.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }
            }
        }

        // --- Enrollment conversations ---
        var enrollmentConversationIds = await _db.EnrollmentConversations
            .Where(c => c.TeacherId == teacherId
                || (enrollmentIds.Count > 0 && enrollmentIds.Contains(c.EnrollmentId)))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (enrollmentConversationIds.Count > 0)
        {
            await _db.EnrollmentConversationMessages
                .Where(m => enrollmentConversationIds.Contains(m.EnrollmentConversationId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.EnrollmentConversations
                .Where(c => enrollmentConversationIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // --- Offer conversations for this teacher ---
        var offerConversationIds = await _db.OfferConversations
            .Where(c => c.TeacherId == teacherId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (offerConversationIds.Count > 0)
        {
            await _db.OfferMessages
                .Where(m => offerConversationIds.Contains(m.OfferConversationId))
                .ExecuteDeleteAsync(cancellationToken);

            await _db.OfferConversations
                .Where(c => offerConversationIds.Contains(c.Id))
                .ExecuteUpdateAsync(
                    s => s.SetProperty(c => c.SessionOfferId, (int?)null),
                    cancellationToken);

            await _db.OfferConversations
                .Where(c => offerConversationIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // --- Enrollment graph ---
        if (enrollmentIds.Count > 0)
        {
            var scheduleIds = await _db.CourseSchedules
                .Where(s => enrollmentIds.Contains(s.EnrollmentId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var slotIds = await _db.EnrollmentSelectedSessionSlots
                .Where(s => enrollmentIds.Contains(s.EnrollmentId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (slotIds.Count > 0)
            {
                await _db.EnrollmentSelectedSessionSlotUnits
                    .Where(u => slotIds.Contains(u.EnrollmentSelectedSessionSlotId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.EnrollmentSelectedSessionSlots
                    .Where(s => slotIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            if (scheduleIds.Count > 0)
            {
                await _db.SessionAttendances
                    .Where(a => scheduleIds.Contains(a.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.SessionLivePresenceEvents
                    .Where(e => scheduleIds.Contains(e.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);

                var homeworkIds = await _db.SessionHomeworkAssignments
                    .Where(h => scheduleIds.Contains(h.CourseScheduleId))
                    .Select(h => h.Id)
                    .ToListAsync(cancellationToken);
                if (homeworkIds.Count > 0)
                {
                    await _db.SessionHomeworkFileLinks
                        .Where(l => homeworkIds.Contains(l.SessionHomeworkAssignmentId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await _db.SessionHomeworkAssignments
                        .Where(h => homeworkIds.Contains(h.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _db.SessionContentLinks
                    .Where(l => scheduleIds.Contains(l.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);

                await _db.CourseSchedules
                    .Where(s => scheduleIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.EnrollmentParticipants
                .Where(p => enrollmentIds.Contains(p.EnrollmentId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Enrollments
                .Where(e => enrollmentIds.Contains(e.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // --- Detach shared pointers ---
        var foreignApprovals = await _db.Enrollments
            .Where(e => e.ApprovedByTeacherId == teacherId
                && e.CourseId != null
                && !courseIds.Contains(e.CourseId.Value))
            .Select(e => new { e.Id, e.CourseId })
            .ToListAsync(cancellationToken);

        foreach (var row in foreignApprovals)
        {
            var ownerTeacherId = await _db.Courses
                .Where(c => c.Id == row.CourseId)
                .Select(c => c.TeacherId)
                .FirstAsync(cancellationToken);
            await _db.Enrollments
                .Where(e => e.Id == row.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.ApprovedByTeacherId, ownerTeacherId),
                    cancellationToken);
        }

        await _db.OpenSessionRequests
            .Where(r => r.TargetedTeacherId == teacherId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.TargetedTeacherId, (int?)null),
                cancellationToken);

        // --- Course enrollment requests on this teacher's courses ---
        if (courseIds.Count > 0)
        {
            var requestIds = await _db.CourseEnrollmentRequests
                .Where(r => courseIds.Contains(r.CourseId))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            if (requestIds.Count > 0)
            {
                var reqSlotIds = await _db.CourseRequestSelectedSessionSlots
                    .Where(s => requestIds.Contains(s.CourseEnrollmentRequestId))
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                if (reqSlotIds.Count > 0)
                {
                    await _db.CourseRequestSelectedSessionSlotUnits
                        .Where(u => reqSlotIds.Contains(u.CourseRequestSelectedSessionSlotId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await _db.CourseRequestSelectedSessionSlots
                        .Where(s => reqSlotIds.Contains(s.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _db.CourseRequestSelectedAvailabilities
                    .Where(a => requestIds.Contains(a.CourseEnrollmentRequestId))
                    .ExecuteDeleteAsync(cancellationToken);

                var proposedIds = await _db.CourseRequestProposedSessions
                    .Where(p => requestIds.Contains(p.CourseEnrollmentRequestId))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);
                if (proposedIds.Count > 0)
                {
                    await _db.CourseRequestProposedSessionUnits
                        .Where(u => proposedIds.Contains(u.CourseRequestProposedSessionId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await _db.CourseRequestProposedSessions
                        .Where(p => proposedIds.Contains(p.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _db.CourseRequestGroupMembers
                    .Where(m => requestIds.Contains(m.CourseEnrollmentRequestId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.CourseEnrollmentRequests
                    .Where(r => requestIds.Contains(r.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var courseSessionIds = await _db.CourseSessions
                .Where(s => courseIds.Contains(s.CourseId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (courseSessionIds.Count > 0)
            {
                await _db.CourseSessionContentLinks
                    .Where(l => courseSessionIds.Contains(l.CourseSessionId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.CourseSessionUnits
                    .Where(u => courseSessionIds.Contains(u.CourseSessionId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.CourseSessions
                    .Where(s => courseSessionIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.Courses
                .Where(c => courseIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // --- Open session participation ---
        if (offerIds.Count > 0)
        {
            await _db.OpenSessionOffers
                .Where(o => offerIds.Contains(o.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.OpenSessionRequestTargets
            .Where(t => t.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);

        // --- Legacy session schema ---
        var legacySessionIds = await _db.Sessions
            .Where(s => s.TeacherId == teacherId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        if (legacySessionIds.Count > 0)
        {
            await _db.ScheduledSessions
                .Where(s => legacySessionIds.Contains(s.SessionId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.Sessions
                .Where(s => legacySessionIds.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.SessionRequestOffers
            .Where(o => o.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);

        // --- Content library ---
        if (contentItemIds.Count > 0)
        {
            await _db.SessionHomeworkFileLinks
                .Where(l => contentItemIds.Contains(l.ContentItemId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.SessionContentLinks
                .Where(l => contentItemIds.Contains(l.ContentItemId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.CourseSessionContentLinks
                .Where(l => contentItemIds.Contains(l.ContentItemId))
                .ExecuteDeleteAsync(cancellationToken);
            await _db.TeacherContentItems
                .Where(c => contentItemIds.Contains(c.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.TeacherContentFolders
            .Where(f => f.TeacherId == teacherId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(f => f.ParentFolderId, (int?)null),
                cancellationToken);
        await _db.TeacherContentFolders
            .Where(f => f.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);

        // --- Registration / docs / subjects ---
        await _db.TeacherRegistrationSubmissions
            .Where(s => s.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherDomainQuestionSubmissions
            .Where(s => s.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherDocuments
            .Where(d => d.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);

        // Clear any remaining Restrict refs to this teacher's availabilities (children first).
        if (availIds.Count > 0)
        {
            var leftoverScheduleIds = await _db.CourseSchedules
                .Where(s => availIds.Contains(s.TeacherAvailabilityId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            if (leftoverScheduleIds.Count > 0)
            {
                await _db.SessionAttendances
                    .Where(a => leftoverScheduleIds.Contains(a.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.SessionLivePresenceEvents
                    .Where(e => leftoverScheduleIds.Contains(e.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);

                var leftoverHomeworkIds = await _db.SessionHomeworkAssignments
                    .Where(h => leftoverScheduleIds.Contains(h.CourseScheduleId))
                    .Select(h => h.Id)
                    .ToListAsync(cancellationToken);
                if (leftoverHomeworkIds.Count > 0)
                {
                    await _db.SessionHomeworkFileLinks
                        .Where(l => leftoverHomeworkIds.Contains(l.SessionHomeworkAssignmentId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await _db.SessionHomeworkAssignments
                        .Where(h => leftoverHomeworkIds.Contains(h.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await _db.SessionContentLinks
                    .Where(l => leftoverScheduleIds.Contains(l.CourseScheduleId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.CourseSchedules
                    .Where(s => leftoverScheduleIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var leftoverReqSlotIds = await _db.CourseRequestSelectedSessionSlots
                .Where(s => availIds.Contains(s.TeacherAvailabilityId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            if (leftoverReqSlotIds.Count > 0)
            {
                await _db.CourseRequestSelectedSessionSlotUnits
                    .Where(u => leftoverReqSlotIds.Contains(u.CourseRequestSelectedSessionSlotId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.CourseRequestSelectedSessionSlots
                    .Where(s => leftoverReqSlotIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var leftoverEnrollSlotIds = await _db.EnrollmentSelectedSessionSlots
                .Where(s => availIds.Contains(s.TeacherAvailabilityId))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
            if (leftoverEnrollSlotIds.Count > 0)
            {
                await _db.EnrollmentSelectedSessionSlotUnits
                    .Where(u => leftoverEnrollSlotIds.Contains(u.EnrollmentSelectedSessionSlotId))
                    .ExecuteDeleteAsync(cancellationToken);
                await _db.EnrollmentSelectedSessionSlots
                    .Where(s => leftoverEnrollSlotIds.Contains(s.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.CourseRequestSelectedAvailabilities
                .Where(a => availIds.Contains(a.TeacherAvailabilityId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await _db.TeacherSubjectUnits
            .Where(u => _db.TeacherSubjects.Any(ts => ts.Id == u.TeacherSubjectId && ts.TeacherId == teacherId))
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherSubjects
            .Where(s => s.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);

        // Explicitly clear cascade children so Teacher delete is clean even if some FKs are Restrict
        await _db.TeacherAvailabilityExceptions
            .Where(e => e.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherAvailabilities
            .Where(a => a.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherAreas
            .Where(a => a.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherReviews
            .Where(r => r.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TeacherAuditLogs
            .Where(a => a.TeacherId == teacherId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
