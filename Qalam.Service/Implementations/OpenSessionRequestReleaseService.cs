using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qalam.Data.Entity.Common.Enums;
using Qalam.Data.Entity.Messaging;
using Qalam.Data.Entity.OpenSessionRequests;
using Qalam.Infrastructure.Abstracts;
using Qalam.Infrastructure.context;
using Qalam.Service.Abstracts;

namespace Qalam.Service.Implementations;

public class OpenSessionRequestReleaseService : IOpenSessionRequestReleaseService
{
    private readonly ApplicationDBContext _db;
    private readonly IOfferConversationRepository _convRepo;
    private readonly ITeacherRepository _teacherRepo;
    private readonly IRabbitMQService _rabbitMq;
    private readonly ILogger<OpenSessionRequestReleaseService> _logger;

    public OpenSessionRequestReleaseService(
        ApplicationDBContext db,
        IOfferConversationRepository convRepo,
        ITeacherRepository teacherRepo,
        IRabbitMQService rabbitMq,
        ILogger<OpenSessionRequestReleaseService> logger)
    {
        _db = db;
        _convRepo = convRepo;
        _teacherRepo = teacherRepo;
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task ReleaseAfterPaymentConflictAsync(
        int enrollmentId,
        CancellationToken cancellationToken = default)
    {
        _db.ChangeTracker.Clear();

        var enrollment = await _db.Enrollments
            .Include(e => e.OpenSessionRequest!)
                .ThenInclude(r => r.Offers)
            .Include(e => e.SelectedSessionSlots)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);

        if (enrollment?.OpenSessionRequest == null || enrollment.SessionOfferId == null)
            return;

        var request = enrollment.OpenSessionRequest;
        var acceptedOfferId = enrollment.SessionOfferId.Value;
        var acceptedOffer = request.Offers.FirstOrDefault(o => o.Id == acceptedOfferId);
        if (acceptedOffer == null)
            return;

        var now = DateTime.UtcNow;
        var conflictDates = enrollment.SelectedSessionSlots?
            .OrderBy(s => s.SessionNumber)
            .Select(s => s.SessionDate.ToString("yyyy-MM-dd"))
            .ToList() ?? [];

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
            enrollment.CancelledAt = now;

            acceptedOffer.Status = OpenSessionOfferStatus.Rejected;
            acceptedOffer.RejectedAt = now;
            acceptedOffer.UpdatedAt = now;

            foreach (var sibling in request.Offers.Where(o =>
                         o.Id != acceptedOfferId
                         && o.Status == OpenSessionOfferStatus.AutoRejected
                         && o.ExpiresAt >= now))
            {
                sibling.Status = OpenSessionOfferStatus.Pending;
                sibling.RejectedAt = null;
                sibling.UpdatedAt = now;
            }

            var hasLivePending = request.Offers.Any(o =>
                o.Status == OpenSessionOfferStatus.Pending && o.ExpiresAt >= now);
            request.Status = hasLivePending
                ? OpenSessionRequestStatus.ReceivingOffers
                : OpenSessionRequestStatus.Active;
            request.UpdatedAt = now;

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        await TryNotifyTeacherAsync(request, acceptedOffer, conflictDates, now, cancellationToken);
    }

    private async Task TryNotifyTeacherAsync(
        OpenSessionRequest request,
        OpenSessionOffer acceptedOffer,
        IReadOnlyList<string> conflictDates,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var datesLabel = conflictDates.Count > 0
            ? string.Join("، ", conflictDates)
            : "المواعيد المحددة";
        var systemMessage =
            $"تعذّر إتمام الدفع — الموعد ({datesLabel}) أصبح محجوزاً. يرجى تقديم عرض جديد بمواعيد مختلفة.";

        try
        {
            var isTargeted = request.TargetedTeacherId != null;
            OfferConversation conv;
            if (isTargeted)
            {
                conv = await _convRepo.EnsureExistsAsync(
                    request.Id, acceptedOffer.TeacherId, cancellationToken);
                if (conv.SessionOfferId != acceptedOffer.Id)
                    await _convRepo.SetCurrentOfferAsync(conv.Id, acceptedOffer.Id, cancellationToken);
            }
            else
            {
                conv = await _convRepo.EnsureExistsForOfferAsync(
                    request.Id, acceptedOffer.TeacherId, acceptedOffer.Id, cancellationToken);
            }

            await _convRepo.AppendMessageAsync(
                conv.Id,
                senderUserId: null,
                OfferMessageType.System,
                systemMessage,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to post payment-conflict system message for offer {OfferId}.",
                acceptedOffer.Id);
        }

        try
        {
            var emails = await _teacherRepo.GetEmailsByTeacherIdsAsync(
                [acceptedOffer.TeacherId], cancellationToken);
            var email = emails.FirstOrDefault(e => e.TeacherId == acceptedOffer.TeacherId).Email;
            if (string.IsNullOrWhiteSpace(email))
                return;

            await _rabbitMq.QueueEmailAsync(new EmailMessage
            {
                To = email,
                Subject = "موعد الجلسة لم يعد متاحاً — يلزم إعادة جدولة العرض",
                Body = systemMessage,
                QueuedAt = now
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to email teacher {TeacherId} about payment conflict on offer {OfferId}.",
                acceptedOffer.TeacherId,
                acceptedOffer.Id);
        }
    }
}
