using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Helpers;
using Qalam.Infrastructure.Abstracts;
using Qalam.Service.Abstracts;

namespace Qalam.Service.BackgroundServices;

public class EnrollmentExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnrollmentExpirationService> _logger;
    private readonly EnrollmentSettings _settings;

    public EnrollmentExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<EnrollmentExpirationService> logger,
        IOptions<EnrollmentSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnrollmentExpirationService started. Check interval: {Minutes} minutes.",
            _settings.ExpirationCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExpireEnrollments(stoppingToken);
                await ExpirePendingGroupInvitations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enrollment expiration check.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.ExpirationCheckIntervalMinutes), stoppingToken);
        }
    }

    private async Task CheckAndExpireEnrollments(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var enrollmentRepo = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();

        var now = DateTime.UtcNow;

        var expired = await enrollmentRepo.GetExpiredPendingPaymentAsync(now, ct);
        foreach (var enrollment in expired)
        {
            enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
            enrollment.CancelledAt = DateTime.UtcNow;
            // CancelledByUserId stays null — cancelled by expiration job.

            // Mark still-pending participants as Cancelled. Already-Succeeded participants
            // stay Succeeded; refund handling is out of scope here.
            foreach (var participant in enrollment.Participants)
            {
                if (participant.PaymentStatus == PaymentStatus.Pending)
                    participant.PaymentStatus = PaymentStatus.Cancelled;
            }

            await enrollmentRepo.UpdateAsync(enrollment);
            _logger.LogInformation("Cancelled expired enrollment {Id} (CourseId: {CourseId}, Kind: {Kind}).",
                enrollment.Id, enrollment.CourseId, enrollment.Kind);
        }

        if (expired.Count > 0)
            await enrollmentRepo.SaveChangesAsync();
    }

    /// <summary>
    /// S1: pending Invited group members past InviteResponseDeadlineHours → Cancelled;
    /// then finalize fixed Approved requests when no pending invitees remain.
    /// </summary>
    private async Task ExpirePendingGroupInvitations(CancellationToken ct)
    {
        var deadlineHours = Math.Max(1, _settings.InviteResponseDeadlineHours);
        var cutoff = DateTime.UtcNow.AddHours(-deadlineHours);

        using var scope = _scopeFactory.CreateScope();
        var requestRepo = scope.ServiceProvider.GetRequiredService<ICourseEnrollmentRequestRepository>();
        var enrollmentRepo = scope.ServiceProvider.GetRequiredService<IEnrollmentRepository>();
        var approvalService = scope.ServiceProvider.GetRequiredService<IEnrollmentApprovalService>();

        var stale = await requestRepo.GetTableAsTracking()
            .Include(r => r.GroupMembers)
            .Include(r => r.Course).ThenInclude(c => c.SessionType)
            .Where(r => r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved)
            .Where(r => r.GroupMembers.Any(gm =>
                gm.MemberType == GroupMemberType.Invited
                && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending
                && gm.CreatedAt < cutoff))
            .ToListAsync(ct);

        if (stale.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var expiredCount = 0;

        foreach (var enrollmentRequest in stale)
        {
            foreach (var gm in enrollmentRequest.GroupMembers
                         .Where(m => m.MemberType == GroupMemberType.Invited
                                     && m.ConfirmationStatus == GroupMemberConfirmationStatus.Pending
                                     && m.CreatedAt < cutoff))
            {
                gm.ConfirmationStatus = GroupMemberConfirmationStatus.Cancelled;
                gm.ConfirmedAt = now;
                expiredCount++;
            }

            // Fixed + Approved: when last pending invite cleared, create PendingPayment if any Confirmed.
            if (enrollmentRequest.Course is { IsFlexible: false }
                && enrollmentRequest.Status == RequestStatus.Approved)
            {
                var stillPendingInvitees = enrollmentRequest.GroupMembers.Any(
                    gm => gm.MemberType == GroupMemberType.Invited
                       && gm.ConfirmationStatus == GroupMemberConfirmationStatus.Pending);

                if (!stillPendingInvitees)
                {
                    var alreadyHasEnrollment = await enrollmentRepo.GetTableNoTracking()
                        .AnyAsync(e => e.EnrollmentRequestId == enrollmentRequest.Id, ct);

                    var hasAnyConfirmedMember = enrollmentRequest.GroupMembers.Any(
                        gm => gm.ConfirmationStatus == GroupMemberConfirmationStatus.Confirmed);

                    if (!alreadyHasEnrollment && hasAnyConfirmedMember)
                    {
                        var paymentDeadline = now.AddHours(_settings.PaymentDeadlineHours);
                        await approvalService.CreatePendingPaymentArtifactsAsync(
                            enrollmentRequest,
                            enrollmentRequest.Course,
                            enrollmentRequest.Course.TeacherId,
                            paymentDeadline,
                            ct);
                    }
                }
            }
        }

        await requestRepo.SaveChangesAsync();
        _logger.LogInformation(
            "Expired {Count} pending group invitations past {Hours}h deadline across {Requests} request(s).",
            expiredCount, deadlineHours, stale.Count);
    }
}
